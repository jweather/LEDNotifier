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

        static int discoflash = 0;
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

                // flash red every 9 cycles if notifier is not connected
                bool flashR = false;
                if (!connected && fade[R] == 100) {
                    discoflash++;
                    flashR = (discoflash % 9) == 0;
                }

                // fade up/down from 80-100% or color cycle
                for (int i = 0; i < 3; i++) {
                    if (up[i]) {
                        fade[i] += 1;
                        if (fade[i] == 100) up[i] = false;
                    } else {
                        fade[i] -= 1;
                        // cycle fades all the way down to 0, normal fades down to 80%
                        if (fade[i] == (cycle ? 0 : 80)) up[i] = true;
                    }
                    uint value = fade[i];
                    if (flashR) {
                        value = (uint)((i == R) ? 100 : 50);
                    }
                    pwm[i].SetPulse(100, value);
                }

                // heartbeat LED    
                led.Write(up[0]);

                // color cycle = fast ramping, normal = slow ramp
                Thread.Sleep(delay / (cycle ? 3 : 1));
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
