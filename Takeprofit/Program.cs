using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Takeprofit
{
    class Program
    {
        static List<int> input = new List<int>();
        static List<int> output = new List<int>();
        static object locker = new object();

        static int totalCount;

        static volatile string key = string.Empty;
        static volatile bool isRegistering = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //ReceiveGreetings();
                
            //Quiz(true);            
            //Check(4925680.5, false);
            Check(4922031.5, true);
        }        

        static void ReceiveKey()
        {
            if (isRegistering)
                return;

            isRegistering = true;

            byte[] data = Encoding.ASCII.GetBytes("Register\n");
            byte[] buffer = new byte[65536];

            try
            {
                while (true)
                {
                    using (TcpClient tcpClient = new TcpClient("88.212.241.115", 2013))
                    {
                        using (NetworkStream stream = tcpClient.GetStream())
                        {
                            stream.Write(data, 0, data.Length);

                            int receivedSize;

                            while ((receivedSize = stream.Read(buffer, 0, tcpClient.ReceiveBufferSize)) > 0)
                            {
                                string receivedMessage = Encoding.GetEncoding("KOI8-R").GetString(buffer, 0, receivedSize);
                                if (!receivedMessage.Contains("Rate limit") && receivedSize >= 16) // отсекаем мусор
                                {
                                    stream.Close();
                                    key = receivedMessage.Replace("\r\n", ""); // WTF?                                    
                                    Console.WriteLine("Актуальный ключ: " + key);
                                    isRegistering = false;
                                    return;
                                }
                            }
                        }                                           
                    }

                    Thread.Sleep(3000); // Сервер не допускает слишком частые запросы ключа
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ReceiveKeyError: " + e.Message);
            }    
            finally
            {
                isRegistering = false;
            }
        }

        static void Quiz(bool isAdvanced)
        {
            for (int i = 1; i <= 2018; i++)
            {
                input.Add(i);
            }

            totalCount = input.Count;

            while (input.Count > 0)
            {
                Console.WriteLine($"Запуск/Перезапуск сбора чисел (найдено {output.Count} из {totalCount})");

                Task[] tasks = new Task[input.Count];

                for (int i = 0; i < input.Count; i++)
                {
                    int inputValue = input[i];
                    tasks[i] = Task.Factory.StartNew(() => ReceiveNumber(inputValue, isAdvanced));
                }

                Task.WaitAll(tasks);
            }

            output.Sort();
            int count = output.Count;
            double median = (output[count / 2 - 1] + output[count / 2]) / 2.0; // медиана для массива чётной длины (2018)

            Console.WriteLine("Median = " + median);
        }

        private static void ReceiveNumber(int inputValue, bool isAdvanced)
        {
            if (isAdvanced && string.IsNullOrEmpty(key))
            {
                ReceiveKey();
                return;
            }

            string requestNumber = isAdvanced ? $"{key}|{inputValue}\n" : $"{inputValue}\n";

            byte[] data = Encoding.ASCII.GetBytes(requestNumber);          

            byte[] buffer = new byte[65536];

            try
            {
                using (TcpClient tcpClient = new TcpClient("88.212.241.115", 2013))
                {
                    using (NetworkStream stream = tcpClient.GetStream())
                    {
                        stream.Write(data, 0, data.Length);

                        string textNumber = string.Empty;
                        int receivedSize;

                        while ((receivedSize = stream.Read(buffer, 0, tcpClient.ReceiveBufferSize)) > 0)
                        {
                            string receivedMessage = Encoding.GetEncoding("KOI8-R").GetString(buffer, 0, receivedSize);

                            if (receivedMessage.Contains("expired"))
                            {
                                ReceiveKey();
                                return;
                            }

                            bool isLineFeed = false;

                            for (int i = 0; i < receivedSize; i++)
                            {
                                byte b = buffer[i];
                                if (b >= 48 && b <= 57)
                                {
                                    string s = Encoding.ASCII.GetString(new byte[] { b });
                                    textNumber += s;
                                }


                                if (b == 10)
                                {
                                    isLineFeed = true;
                                    break;
                                }
                            }

                            if (isLineFeed)
                            {
                                if (int.TryParse(textNumber, out int result))
                                {
                                    lock (locker)
                                    {
                                        output.Add(result);
                                        input.Remove(inputValue);
                                    }

                                    Console.WriteLine($"Progress: {output.Count}/{totalCount}");
                                }
                                break;
                            }
                        }
                    }                
                }
               
            }
            catch (Exception e)
            {
                Console.WriteLine("ReceiveNumberError: " + e.Message);               
            }
        }

        static void Check(double value, bool isAdvanced)
        {
            using (TcpClient tcpClient = new TcpClient("88.212.241.115", 2013))
            {
                string checkAnswer = isAdvanced ? $"Check_Advanced {value}\n" : $"Check {value}\n";
                byte[] data = Encoding.ASCII.GetBytes(checkAnswer);

                NetworkStream stream = tcpClient.GetStream();
                stream.Write(data, 0, data.Length);

                byte[] buffer = new byte[65536];

                int receivedSize;

                while ((receivedSize = stream.Read(buffer, 0, tcpClient.ReceiveBufferSize)) > 0)
                {
                    string receivedMessage = Encoding.GetEncoding("KOI8-R").GetString(buffer, 0, receivedSize);
                    Console.WriteLine(receivedMessage);
                }

                stream.Close();
            }
        }

        static void ReceiveGreetings()
        {
            using (TcpClient tcpClient = new TcpClient("88.212.241.115", 2013))
            {
                byte[] data = Encoding.ASCII.GetBytes("Greetings\n");

                NetworkStream stream = tcpClient.GetStream();
                stream.Write(data, 0, data.Length);

                byte[] buffer = new byte[65536];

                int receivedSize;

                while ((receivedSize = stream.Read(buffer, 0, tcpClient.ReceiveBufferSize)) > 0)
                {
                    string receivedMessage = Encoding.GetEncoding("KOI8-R").GetString(buffer, 0, receivedSize);
                    Console.WriteLine(receivedMessage);
                }                

                stream.Close();
            }           
        }
    }
}
