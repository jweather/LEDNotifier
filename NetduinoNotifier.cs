using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

/* Jeremy Weatherford
 * no rights reserved
 */

namespace HelloPWM {
    public class Program {
        const int G = 0, R = 1, B = 2; // the order my PWM pins are wired up
        static int delay = 50;
        static bool cycle = false, pause = false;
        static bool[] up = new bool[] { true, true, true };
        static uint[] fade = new uint[] { 0, 0, 0 };
        static PWM[] pwm;

        static bool connected = false;

        public static void Main() {
            pwm = new PWM[]{new PWM(Pins.GPIO_PIN_D5),
                new PWM(Pins.GPIO_PIN_D6),
                new PWM(Pins.GPIO_PIN_D9)};

            OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);

            Thread t = new Thread(new ThreadStart(listener));
            t.Start();

            while (true) {
                while (pause)
                    Thread.Sleep(50);

                // fade up/down from 80-100% or color cycle
                uint min = 80, max = 100;
                if (!connected) {
                    min = 1; max = 1;
                } else if (cycle) {
                    min = 0; max = 100;
                }
                for (int i = 0; i < 3; i++) {
                    if (up[i]) {
                        if (fade[i] < max) fade[i] += 1;
                        if (fade[i] >= max) up[i] = false;
                    } else {
                        if (fade[i] > min) fade[i] -= 1;
                        if (fade[i] <= min) up[i] = true;
                    }
                    uint value = fade[i];
                    pwm[i].SetPulse(100, value);
                }

                // heartbeat LED    
                led.Write(up[0]);

                // color cycle = fast ramping, normal = slow ramp
                Thread.Sleep(delay / (cycle ? 4 : 1));
            }
        }
        private static void listener() {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Bind(new IPEndPoint(IPAddress.Any, 23));
            Debug.Print(Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress);
            sock.Listen(10);

            while (true) {
                using (Socket cli = sock.Accept()) {
                    Debug.Print("connected");
                    connected = true;
                    try {
                        NetworkStream ns = new NetworkStream(cli);
                        StreamReader sr = new StreamReader(ns);
                        while (true) {
                            string line = sr.ReadLine();
                            if (line == null || line == "") break;
                            parse(line);
                        }
                    } catch (Exception e) { Debug.Print(e.Message);  }
                    try { cli.Close(); } catch { }
                    Debug.Print("disconnected");
                    connected = false;
                }
            }
        }

        private static void parse(string request) {
            Debug.Print("RX " + request);
            if (request == "outlook on" && !cycle) {
                // start a color cycle by setting the three counters 100/3 values apart
                cycle = true;
                fade[0] = 0; fade[1] = 33; fade[2] = 66;
                up[0] = true; up[1] = true; up[2] = true;
            } else if (request == "outlook off" && cycle) {
                // back to normal, everything = 80% to start with
                cycle = false;
                fade[0] = 80; fade[1] = 80; fade[2] = 80;
                up[0] = true; up[1] = true; up[2] = true;
            } else if (request == "flash on") {
                // interrupt the normal fading
                pause = true;

                // wake up!
                for (int i = 15; i < 20; i++) {
                    for (int j = 0; j < 3; j++) {
                        pwm[j].SetPulse(100, 100);
                    }
                    Thread.Sleep(500 / i);
                    for (int j = 0; j < 3; j++) {
                        pwm[j].SetPulse(100, 0);
                    }
                    Thread.Sleep(200 / i);
                }
                pause = false;
            }
        }

    }
}
