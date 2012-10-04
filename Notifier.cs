using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Outlook = Microsoft.Office.Interop.Outlook;

/* 
 * Jeremy Weatherford
 * no rights reserved
 */

/* TODO
 * GUI for settings and feature selection
 * RGB picker for background color, maybe select "color walk" keyframes
 * network reachability notifier
 * AWS status
 * GMail xidus.net and carrotsaver
 * music beat detection
 * sunrise/sunset simulation
 */

namespace Notifier {
    class Program {
        static void Main(string[] args) {
            Program p = new Program();
        }

        TcpClient sockNotify;
        StreamWriter notifySW;

        Program() {
            System.Timers.Timer t = new System.Timers.Timer(5000);
            t.AutoReset = true;
            t.Elapsed += new System.Timers.ElapsedEventHandler(t_Elapsed);
            t.Start();

            Thread connect = new Thread(new ThreadStart(connectThread));
            connect.IsBackground = true;
            connect.Start();

            TcpListener server = new TcpListener(IPAddress.Loopback, 8003);
            server.Start();
            d("listening on :8003");
            while (true) {
                TcpClient cli = server.AcceptTcpClient();
                string input = new StreamReader(cli.GetStream()).ReadLine();
                d("RX: " + input);
                if (input.StartsWith("flash ")) {
                    sendNotify("flash on");
                }
                cli.Close();
            }
        }

        void connectThread() {
            while (true) {
                try {
                    sockNotify = new TcpClient();
                    sockNotify.Connect("192.168.1.10", 23);
                    if (sockNotify.Connected) {
                        d("connected");
                        notifySW = new StreamWriter(sockNotify.GetStream());
                        notifySW.AutoFlush = true;
                        while (sockNotify.Connected) {
                            notifySW.WriteLine("ping");
                            Thread.Sleep(5000);
                        }
                    }
                } catch { }
                try { sockNotify.Close(); } catch { }
                Thread.Sleep(5000);
                d("Reconnecting");
            }
        }

        void sendNotify(string msg) {
            try {
                notifySW.WriteLine(msg);
            } catch { }
        }

        void t_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            if (checkOutlook()) {
                sendNotify("outlook on");
            } else {
                sendNotify("outlook off");
            }
        }

        private bool checkOutlook() {
            try {
                Outlook._Application app = new Outlook.Application();
                Outlook.NameSpace ns = app.GetNamespace("MAPI");
                Outlook.MAPIFolder f = ns.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderInbox);
                return f.UnReadItemCount > 0;
            } catch (Exception e) {
                d(e.Message + e.StackTrace);
                return true;
            }

        }

        private void d(string msg) { Console.WriteLine(msg); }
    }
}
