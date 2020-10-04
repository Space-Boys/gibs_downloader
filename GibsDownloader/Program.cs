using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections;


namespace GibsDownloader
{
    class Program
    {
        

        static void Main(string[] args)
        {
            new Program(args);
        }


        Semaphore semaphore;


        public Program(string[] args)
        {
            int threads = 1;
            

            String date = args[0];
            int tileMatrix = Int32.Parse(args[1]);
            try
            {
                threads = Int32.Parse(args[2]);
            }
            catch { }

            semaphore = new Semaphore(threads, threads);

            Console.WriteLine("Threads: " + threads);
            //Thread.Sleep(2000);

            Console.WriteLine("Downloading Date: " + date + ", TileMatrix: " + tileMatrix);

            double tileSize = 288 / Math.Pow(2, tileMatrix);

            int maxCol = (int)(360 / tileSize);
            int maxRow = (int)(180 / tileSize);


            String url = "https://gibs-{a-c}.earthdata.nasa.gov/wmts/epsg4326/best/wmts.cgi?TIME="+date+ "T00:00:00Z&layer=VIIRS_SNPP_CorrectedReflectance_TrueColor&style=default&tilematrixset=250m&Service=WMTS&Request=GetTile&Version=1.0.0&Format=image/jpeg&TileMatrix="+ tileMatrix + "&TileCol={TileCol}&TileRow={TileRow}";

            int totalFiles = maxRow * maxCol;

            DirectoryInfo outDi = new DirectoryInfo("out");
            if (!outDi.Exists)
            {
                outDi.Create();
            }

            DirectoryInfo dateDi = new DirectoryInfo(outDi.FullName+"\\" +date);
            if (!dateDi.Exists)
            {
                dateDi.Create();
            }

            DirectoryInfo tileDi = new DirectoryInfo(dateDi .FullName + "\\TileMatrix"+tileMatrix);
            if (!tileDi.Exists)
            {
                tileDi.Create();
            }

            char[] serverVariation = new char[] { 'a', 'b', 'c' };
            Random r = new Random();


            for (int row = 0; row < maxRow; row++)
            {
                for (int col = 0; col < maxCol; col++)
                {
                    FileInfo fileInfo = new FileInfo(tileDi.FullName + "\\" + row + "_" + col + ".jpg");

                    int currentFile = row * maxCol + col;
                    double progress = ((double)currentFile) / totalFiles;

                    String downloadUrl = url.Replace("{TileCol}", "" + col)
                        .Replace("{TileRow}", "" + row).
                        Replace("{a-c}",""+serverVariation[r.Next()%3]);
                    

                    //Console.WriteLine(String.Format("{0:P2}.", progress)+"\t"+downloadUrl);

                    Console.Write(String.Format("{0:P2}.", progress) + "\t" + date+"\t"+tileMatrix+"\t"+row+"\t"+col+"\t");

                    if (fileInfo.Exists && fileInfo.Length > 0)
                    {
                        Console.WriteLine("File exists, " + fileInfo.Length + " bytes, skipping...");
                        continue;
                    } else
                    {
                        Console.Write("Download...\t");
                        
                        DownloadTask downloadTask = new DownloadTask() { FileName = fileInfo.FullName, Url = downloadUrl };

                        semaphore.WaitOne();
                        Thread t = new Thread(new ParameterizedThreadStart(Worker));
                        t.Start(downloadTask);
                    }

                }
            }

            for (int i=0;i< threads; i++)
            {
                semaphore.WaitOne();
            }

        }


        private void Worker(object task)
        {
            try
            {
                DownloadTask downloadTask = (DownloadTask)task;
                WebClient webClient = new WebClient();
                webClient.DownloadFile(downloadTask.Url, downloadTask.FileName);
                Console.WriteLine("Downloading ..." + downloadTask.Url);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
            finally
            {
                semaphore.Release();
            }
        }

    }

    //Console.WriteLine("Done");
    ///
}
