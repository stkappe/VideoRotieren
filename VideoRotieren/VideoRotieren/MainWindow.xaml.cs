using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VideoRotieren
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Zwischenspeicherung nach Klick auf OK im InputFileDialog
        Queue<String> tempInputFileNames = new Queue<String>();
        Queue<String> inputFileNames = new Queue<String>();
        String tempOutputFolder = "";
        String outputFolder = "";
        // Zwischenspeicherung nach Klick auf OK im OutputFileDialog
        Queue<String> tempOutputFileNames = new Queue<String>();
        Queue<String> outputFileNames = new Queue<String>();

        int degrees;

        int duration_hours, duration_mins, duration_secs, duration_millisecs;
        int current_hours, current_mins, current_secs, current_millisecs;
        private DispatcherTimer timer;
        private static Process process;
        System.Windows.Controls.ToolTip tooltip_textBox_customDegrees;

        

        public MainWindow()
        {
            InitializeComponent();
            textBlock_rotate_success.Visibility = System.Windows.Visibility.Hidden;
            statusBar1.Visibility = System.Windows.Visibility.Hidden;

            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

            // Tooltip für Gradzahleingabe TextBox
            tooltip_textBox_customDegrees = new System.Windows.Controls.ToolTip { Content = "Bitte Zahl zwischen 1 und 359 eingeben" };
            textBox_customDegrees.ToolTip = tooltip_textBox_customDegrees;
            tooltip_textBox_customDegrees.IsOpen = false;

            InitializeMyTimer();
        }

        private void button_browseInputFile_Click(object sender, RoutedEventArgs e)
        {
            tempInputFileNames.Clear();
            tempOutputFileNames.Clear();
            tempOutputFolder = "";


            // OpenFileDialog, mehrere Dateien auswählbar, nur Videodateien auswählbar
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Videodateien (*.mp4;*.avi;*.mpg;*.mpeg;*.mkv;*.flv;*.mov;*.3gp)|*.mp4;*.avi;*.mpg;*.mpeg;*.mkv;*.flv;*.mov;*.3gp";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                // Für jeden eingegebenen Dateinamen...
                for (int i = 0; i < openFileDialog.FileNames.Length; i++)
                {
                    // hole i-ten eingegebenen Dateinamen
                    String fileName = openFileDialog.FileNames[i];
                    Console.WriteLine("openFileDialog.FileNames.Length: " + openFileDialog.FileNames.Length);
                    Console.WriteLine("inputfilename: " + fileName);

                    // Dateiname (z.B. hallo.mp4) ohne Ordner und Slashes "\"
                    String[] stringSeparators = new string[] { "\\" };
                    String[] result = fileName.Split(stringSeparators, StringSplitOptions.None);

                    if (!(String.IsNullOrEmpty(fileName)))
                    {
                        // Im Array, das die Eingabedateinamen sammelt, den Dateinamen abspeichern
                        tempInputFileNames.Enqueue(fileName);
                        label_currentFile.Content = "Datei: " + result[result.Length - 1];
                        if (openFileDialog.FileNames.Length > 1)
                            label_currentFile.Content += " + " + Convert.ToString(openFileDialog.FileNames.Length-1) + " weitere Dateien.";

                        // mediaElement_displayVideo.Source = new Uri(chosenFile, UriKind.Absolute);
                        // mediaElement_displayVideo.Play();
                    }
                }
            }
        }

        private void button_browseOutputFile_Click(object sender, RoutedEventArgs e)
        {


            // Wenn noch keine Eingabedatei festgelegt wurde, erscheint Fehlermeldung
            if (tempInputFileNames.Count == 0 || String.IsNullOrEmpty(tempInputFileNames.Peek()))
            {
               MessageBoxResult result = System.Windows.MessageBox.Show("Bitte zuerst eine Eingabedatei festlegen!", "Achtung", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {

                // Wird nur eine Datei eingegeben, kann der User den Ausgabedateinamen und den Speicherort selbst bestimmen.
                if (tempInputFileNames.Count == 1)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "Videodateien(*.mp4; *.avi; *.mpg; *.mpeg; *.mkv; *.flv; *.mov; *.3gp)| *.mp4; *.avi; *.mpg; *.mpeg; *.mkv; *.flv; *.mov; *.3gp";

                    String[] stringSeparators = new string[] { "\\" };

                    // Jeder Dateiname, z.B. hallo.mp4, wird um _rotiert erweitert, also hallo_rotiert.mp4
                    // Zuerst nach Slash "\" trennen, um an den eigentlichen Dateinamen ohne Ordner zu kommen
                    String[] result = tempInputFileNames.Peek().Split(stringSeparators, StringSplitOptions.None);
                    String tmp = result[result.Length - 1];
                    saveFileDialog.FileName = tmp.Substring(0, tmp.Length - 4) + // "hallo" (von hallo.mp4)
                        " rotiert" + // "hallo_rotiert"
                        tmp.Substring(tmp.Length - 4, 4); // "hallo_rotiert.mp4"


                    if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        // User hat die Möglichkeit, die vorgeschlagenen Namen im outputFile-Array
                        // und in saveFileDialog.FileName:
                        // hallo_rotiert.mp4, aufnahme_rotiert.mp4, usw.
                        // zu akzeptieren, oder einen neuen Namen einzugeben
                        if (!(String.IsNullOrEmpty(saveFileDialog.FileName)))
                        {

                            // Der Nutzer könnte den Namen gegenüber dem Vorschlag nocheinmal verändert haben
                            tempOutputFileNames.Enqueue(saveFileDialog.FileName);

                            String[] stringSeparators2 = new string[] { "\\" };
                            String[] result2 = tempOutputFileNames.Peek().Split(stringSeparators, StringSplitOptions.None);

                            Console.WriteLine("outputfilename: " + tempOutputFileNames.Peek() + " result2: " + result2[result2.Length - 1]);

                            label_outputFile.Content = "Datei: " + result2[result2.Length-1];
                        }
                    }
                }

                // Es wurde mehr als 1 Datei als Input gegeben.
                // In diesem Fall kann der User den Namen der Ausgabedatei nicht selbst bestimmen, 
                // sondern er muss dann einen Ordner angeben, an dem die Ausgabedateien unter ihrem
                // Originalnamen gespeichert werden.
                else
                {

                    FolderBrowserDialog fbd = new FolderBrowserDialog();
                    
                    String[] stringSeparators = new string[] { "\\" };
                    String[] result2 = tempInputFileNames.Peek().Split(stringSeparators, StringSplitOptions.None);

                    // Der beinhaltende Ordner der ersten eingegebenen Datei in inputFile[0]
                    fbd.SelectedPath = System.IO.Path.GetDirectoryName(tempInputFileNames.Peek().Substring(0, tempInputFileNames.Peek().Length-result2[result2.Length-1].Length));
                    fbd.Description = "Ausgabeordner wählen";

                    if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        if (!(String.IsNullOrEmpty(fbd.SelectedPath)))
                        {
                            tempOutputFolder = fbd.SelectedPath + "\\rotiert\\";
                            Console.WriteLine("tempOutputFolder: " + tempOutputFolder);
                        }
                    }                
            
                }
            }
        }

        private void button_executeProcess_Click(object sender, RoutedEventArgs e)
        {

            if(tempInputFileNames == null || !tempInputFileNames.Any() || String.IsNullOrEmpty(tempInputFileNames.Peek()))
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("Bitte zuerst Eingabedatei festlegen!", "Achtung", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (tempOutputFileNames == null || (!tempInputFileNames.Any() && String.IsNullOrEmpty(tempOutputFolder)))
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("Bitte zuerst Ausgabedatei festlegen!", "Achtung", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (radio_90_mit_UZS.IsChecked == false && radio_90_gegen_UZS.IsChecked == false && radio_180.IsChecked == false && radio_customDegrees.IsChecked == false)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("Bitte Rotation festlegen!", "Achtung", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (radio_customDegrees.IsChecked == true && (degrees < 1 || degrees > 359))
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("Bitte einen Rotationswert zwischen 1 und 359 Grad eingeben!", "Achtung", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            textBlock_rotate_success.Visibility = System.Windows.Visibility.Hidden;
            statusBar1.Visibility = System.Windows.Visibility.Visible;
            button_browseInputFile.IsEnabled = false;
            button_browseOutputFile.IsEnabled = false;
            grid_radio_inputTurnDegree.IsEnabled = false;


            foreach (String str_tmp_in in tempInputFileNames)
            {
                inputFileNames.Enqueue(str_tmp_in);
            }
            foreach (String str_tmp_out in tempOutputFileNames)
            {
                outputFileNames.Enqueue(str_tmp_out);
            }
            outputFolder = tempOutputFolder;

            tempInputFileNames.Clear();
            tempOutputFileNames.Clear();
            tempOutputFolder = "";

            

            if (outputFolder != null && outputFolder != "")
                System.IO.Directory.CreateDirectory(outputFolder);

            degrees = 0;

            // transpose
            // 0 = 90CounterCLockwise and Vertical Flip (default)
            // 1 = 90Clockwise
            // 2 = 90CounterClockwise
            // 3 = 90Clockwise and Vertical Flip
            if (radio_90_mit_UZS.IsChecked == true) degrees = 90;
            else if (radio_90_gegen_UZS.IsChecked == true) degrees = -90;
            else if (radio_180.IsChecked == true) degrees = 180;
            else if (radio_customDegrees.IsChecked == true) degrees = Convert.ToInt32(textBox_customDegrees.Text);

            if (inputFileNames.Count > 1)
            {
                // outputFileNames ist noch leer, da bisher nur der Ausgabeordner angegeben wurde
                // Falls mehrere Dateien rotiert werden sollen, wird kein Outputfilename vom User angegeben, sondern
                // stattdessen nur der beinhaltende Output-Ordner. Das Programm sucht mit "GetNextAvailableFilename"
                // für jede Inputdatei nach einem Ausgabedateinamen
                for (int i = 0; i < inputFileNames.Count; i++)
                {
                    GetNextAvailableFilename(inputFileNames.ElementAt(i), outputFileNames);
                }

                Console.WriteLine("arguments: -i \"" + inputFileNames.Peek() + "\" -vf \"rotate = " + degrees + " * (PI / 180):bilinear = 0\" -codec:a copy \"" + outputFileNames.Peek());

                Console.WriteLine("button_executeProcess_Click inputFileNames Number: " + inputFileNames.Count);
                Console.WriteLine("button_executeProcess_Click outputFileNames Number: " + outputFileNames.Count);
                Console.WriteLine("button_executeProcess_Click folder name: " + outputFolder);
 
                startNewProcess("-i \"" + inputFileNames.Dequeue() + "\" -vf \"rotate = " + degrees + " * (PI / 180):bilinear = 0\" -y -codec:a copy \"" + outputFileNames.Dequeue());
            }

            else
            {
                Console.WriteLine("arguments: -i \"" + inputFileNames.Peek() + "\" -vf \"rotate = " + degrees + " * (PI / 180):bilinear = 0\" -codec:a copy \"" + outputFileNames.Peek());

                startNewProcess("-i \"" + inputFileNames.Dequeue() + "\" -vf \"rotate = " + degrees + " * (PI / 180):bilinear = 0\" -y -codec:a copy \"" + outputFileNames.Dequeue());
            }
        }

        public void hallo(String hallostring)
        {
            Console.WriteLine("hallo inputFileNames Number: " + inputFileNames.Count);
            Console.WriteLine("hallo outputFileNames Number: " + outputFileNames.Count);
            Console.WriteLine("hallo folder name: " + outputFolder);
        }
        
        public void GetNextAvailableFilename(string filename, Queue<String> outputFileNames)
        {
                string alternateFilename;
                int fileNameIndex = 1;
                do
                {
                    fileNameIndex += 1;
                    alternateFilename = CreateNumberedFilename(filename, fileNameIndex);
                } while ((System.IO.File.Exists(outputFolder + alternateFilename))
                // } while ((System.IO.File.Exists(System.IO.Path.GetDirectoryName(filename) + "\\" + alternateFilename))
                        || outputFileNames.Contains(filename));

                Console.WriteLine("Filename: " + filename + " alternateFilename: " + alternateFilename);
                outputFileNames.Enqueue(outputFolder + alternateFilename);
        }

        private string CreateNumberedFilename(string filename, int number)
        {
            string plainName = System.IO.Path.GetFileNameWithoutExtension(filename);
            string extension = System.IO.Path.GetExtension(filename);
            return string.Format("{0}{1}{2}", plainName, number, extension);
        }

        void startNewProcess(String arguments)
        {
            // Timer muss hier initialisiert werden, da ein Timer mehrmals gestartet und gestoppt werden
            // muss, wenn mehrere Videos rotiert werden, ohne das Programm neuzustarten

            Console.WriteLine("startNewProcess inputFileNames Number: " + inputFileNames.Count);
            Console.WriteLine("startNewProcess outputFileNames Number: " + outputFileNames.Count);
            Console.WriteLine("startNewProcess folder name: " + outputFolder);

            process = new Process();
            //process.StartInfo.FileName = "cmd.exe";
            //process.StartInfo.Arguments = "/c DIR";
            process.StartInfo.FileName = "ffmpeg.exe";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.EnableRaisingEvents = true;
            //* Set your output and error (asynchronous) handlers
            process.OutputDataReceived += new DataReceivedEventHandler(ConsoleOutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(ConsoleOutputHandler);
            process.Exited += new EventHandler(ProcessExitHandler);
            //* Start process and handlers
            try
            {
                statusBar1.Visibility = System.Windows.Visibility.Visible;
                textBlock_rotate_success.Visibility = System.Windows.Visibility.Hidden;

                timer.Start();

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                Console.WriteLine("Button Execute Process Click!!");
            }
            catch (Exception exception)
            {
                MessageBoxResult msgBoxResult = System.Windows.MessageBox.Show("Ein Fehler ist aufgetreten:    " + exception, "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Methode wird nach dem Rotieren jedes Videos aufgerufen
        void ProcessExitHandler(object sender, System.EventArgs e)
        {
            Console.WriteLine("ConsoleExitHandler!!!");
            Console.WriteLine("ConsoleExitHandler!!!");
            Console.WriteLine("ConsoleExitHandler!!!");

            timer.Stop();

            Console.WriteLine("ProcessExitHandler inputFileNames Number: " + inputFileNames.Count);
            Console.WriteLine("ProcessExitHandler outputFileNames Number: " + outputFileNames.Count);

            // Nacharbeiten nach Beendigung des Programms
            Dispatcher.BeginInvoke(
                  new Action(() => {
                      statusBar1.Visibility = System.Windows.Visibility.Hidden;
                      textBlock_rotate_success.Visibility = System.Windows.Visibility.Visible;
                      label_currentFile.Content = "Ausgewählte Datei: ";
                      label_outputFile.Content = "Ausgewählte Datei: ";

                      progressBar1.Value = 0;
                      statusBar_textBlock_progressPercent.Text = progressBar1.Value.ToString() + "%";

                      if (inputFileNames.Any() && outputFileNames.Any())
                      {
                          // nach einem Prozessdurchlauf sind immer noch InputFiles vorhanden. 
                          // D.h. Es wurde zu Anfang mehr als ein zu rotierendes Video angegeben. 
                          // D.h. outputfolder ist nicht null und
                          // GetNextAvailableFilename() wurde durchlaufen
                          Console.WriteLine("-i \"" + inputFileNames.Peek() + "\" -vf \"rotate = " + degrees + " * (PI / 180):bilinear = 0\" -y -codec:a copy \""  + outputFileNames.Peek() + "\"");

                          startNewProcess("-i \"" + inputFileNames.Dequeue() + "\" -vf \"rotate = " + degrees + " * (PI / 180):bilinear = 0\" -y -codec:a copy \""  + outputFileNames.Dequeue() + "\"");
                      }
                      else {
                          inputFileNames.Clear();
                          outputFileNames.Clear();
                          outputFolder = "";
                          degrees = 0;

                          button_browseInputFile.IsEnabled = true;
                          button_browseOutputFile.IsEnabled = true;
                          grid_radio_inputTurnDegree.IsEnabled = true;
                      }
                      
                  })
            );

            Console.WriteLine("END OF PROGRAM!");
            Console.WriteLine("END OF PROGRAM!");
            Console.WriteLine("END OF PROGRAM!");
        }

        void ConsoleOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            String line = outLine.Data;
            Console.WriteLine(line);

            if (line != null && line.Contains("duration") | line.Contains("Duration"))
            {
                // Duration: 00:03:41.97, start: 0.036281, bitrate: 765 kb/s
                // result hat dann 3 Eintraege, am Komma gesplittet
                String[] result = line.Split(',');
                foreach (String s in result)
                {
                    if (s.Contains("duration") | s.Contains("Duration"))
                    {
                        // aus "Duration: 00:03:41.97" wird "00:03:41.97"
                        string duration = s.Substring(11);
                        String[] result2 = duration.Split(':');
                        Console.WriteLine("hours: " + result2[0] + ", mins: " + result2[1] + ", secs: " + result2[2]);
                        this.duration_hours = Convert.ToInt32(result2[0]);
                        this.duration_mins = Convert.ToInt32(result2[1]);
                        this.duration_secs = Convert.ToInt32(result2[2].Split('.')[0]);
                        this.duration_millisecs = Convert.ToInt32(result2[2].Split('.')[1]);
                        Console.WriteLine("hours: " + this.duration_hours + ", mins: " + this.duration_mins +
                            ", secs: " + this.duration_secs + ", millisecs: " + this.duration_millisecs);
                    }
                }

            }
            if (line != null && line.StartsWith("frame="))
            {
                String[] result = line.Split(' ');
                String time = "";

                // Beispielzeile:
                // frame=  138 fps=136 q=28.0 size=     137kB time=00:00:05.66 bitrate= 198.4kbits/s dup=1 drop=0  
                // result enthält auch Einträge mit einem Leerzeichen. 
                // Manchmal sind mehr, manchmal weniger Leerzeichen in einer Zeile.
                // Daher ist für time kein fester Zugriff auf ein result-Element möglich.
                // Es wäre auch ein Zugriff auf Zeichen 63-79 einer jeden Zeile möglich. Aber womöglich auch unsicher.
                foreach (String s in result)
                {
                    if (s.StartsWith("time"))
                    {
                        time = s;
                        break;
                    }
                }
                Console.WriteLine("time: " + time);

                string duration = time.Substring(5);
                String[] result2 = duration.Split(':');
                current_hours = Convert.ToInt32(result2[0]);
                current_mins = Convert.ToInt32(result2[1]);
                current_secs = Convert.ToInt32(result2[2].Split('.')[0]);
                current_millisecs = Convert.ToInt32(result2[2].Split('.')[1]);

                Console.WriteLine("current_hours: " + current_hours + "," + current_mins + "," + current_secs + "," + current_millisecs);


            }

            if (!(String.IsNullOrEmpty(line)) && (
                line.Contains("Invalid data found when processing input") |
                line.Contains("Internal bug, also see AVERROR_BUG2") |
                line.Contains("Buffer too small") |
                line.Contains("Decoder not found") |
                line.Contains("Demuxer not found") |
                line.Contains("Encoder not found") |
                line.Contains("End of file") |
                line.Contains("Immediate exit was requested; the called function should not be restarted") |
                line.Contains("Generic error in an external library") |
                line.Contains("Filter not found") |
                line.Contains("Invalid data found when processing input") |
                line.Contains("Muxer not found") |
                line.Contains("Option not found") |
                line.Contains("Not yet implemented in FFmpeg") |
                line.Contains("Protocol not found") |
                line.Contains("Stream not found") |
                line.Contains("AVERROR_BUG") |
                line.Contains("Unknown error")))
            {
                System.Windows.MessageBox.Show("Ein Fehler ist aufgetreten: " + line, "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                statusBar1.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        // Überprüft jede 1 Sekunde, wie die Relation von benötigter Zeit im Konvertierungsprozess zur Gesamtlänge der Videodatei ist.
        // Setzt den Wert der ProgressBar auf vergangene Zeit geteilt durch Gesamtlänge der Videodatei
        private void timer_Tick(object sender, EventArgs e)
        {
            Console.WriteLine("Timer Tick!");
            Console.WriteLine("duration_hours: " + duration_hours + "," + duration_mins + "," + duration_secs + "," + duration_millisecs);
            Console.WriteLine("current_hours: " + current_hours + "," + current_mins + "," + current_secs + "," + current_millisecs);
            // wenn die current-Werte = 0 sind, ist der Verarbeitungsprozess noch nicht angefangen
            // und die ProgressBar darf sich nicht verändern
            if (current_hours != 0 || current_mins != 0 || current_mins != 0 || current_secs !=0)
            {
                double total_duration_secs = duration_hours * 3600 + duration_mins * 60 + duration_secs;
                double total_current_secs = current_hours * 3600 + current_mins * 60 + current_secs;

                Console.WriteLine("Set ProgressBar Value to: " + (int)(total_current_secs / total_duration_secs * 100));
                this.progressBar1.Value = (int)(total_current_secs / total_duration_secs * 100);

                statusBar_textBlock_progressPercent.Text = progressBar1.Value.ToString() + "%";
                // Determine if we have completed by comparing the value of the Value property to the Maximum value.
                if (progressBar1.Value == progressBar1.Maximum)
                {
                    Console.WriteLine("Timer last Tick!!");
                   
                }
            }
        }

        private void InitializeMyTimer()
        {
            this.timer = new DispatcherTimer();
            this.timer.Tick += timer_Tick;
            this.timer.Interval = new System.TimeSpan(0, 0, 1);
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
        }


        // Bei Schließen vom MainWindow
        static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("OnProcessExit!!");
            Process[] processes = Process.GetProcessesByName("ffmpeg");
            if (processes.Length > 0)
            {
                process.Kill();
                process.Dispose();
            }
                
        }

        private void radio_customDegrees_Changed(object sender, RoutedEventArgs e)
        {
            //if (radio_customDegrees.IsChecked.HasValue)
            //{
                textBox_customDegrees.IsEnabled = (bool)radio_customDegrees.IsChecked;
                radio_customDegrees_imUZS.IsEnabled = (bool)radio_customDegrees.IsChecked;
                radio_customDegrees_gegenUZS.IsEnabled = (bool)radio_customDegrees.IsChecked;

                tooltip_textBox_customDegrees.IsOpen = false;
            //}

        }

        private void textBox_customDegrees_previewTextInput(object sender, TextCompositionEventArgs e)
        {
            // nur Zahlen,+ und - erlaubt
            Regex regex = new Regex("[a-zA-Z*/_:;.,<>|!\"§$% &/ () =?`´{}\\^°~#'@²³+-]"); //regex that matches disallowed text
            e.Handled = regex.IsMatch(e.Text);
            Console.WriteLine("handled: " + e.Handled + ", getType: " + e.Text.GetType());      
        }

        // TextChanged="textBox_customDegrees_TextChanged"
        private void textBox_customDegrees_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox_customDegrees.Text))
            {
                if (((Convert.ToInt32(textBox_customDegrees.Text)) >= 360) || ((Convert.ToInt32(textBox_customDegrees.Text)) == 0))
                {
                    tooltip_textBox_customDegrees.IsOpen = true;
                    textBox_customDegrees.BorderBrush = Brushes.Red;
                    textBox_customDegrees.BorderThickness = new Thickness(2.0);
                }
                else
                {
                    tooltip_textBox_customDegrees.IsOpen = false;
                    textBox_customDegrees.BorderBrush = Brushes.Gray;
                    textBox_customDegrees.BorderThickness = new Thickness(1.0);
                }
            }  
        }
        /*C: \Users\Steffen\Downloads\ffmpeg - 2.8win\bin > ffmpeg - i "C:\Users\Steffen\Downloa
    ds\Milky Chance -Stolen Dance.mp4" -vf "rotate = 180 * (PI / 180):bilinear = 0" -codec:
    a copy "C:\Users\Steffen\Downloads\gedreht3.mp4"*/
        //}


    }

}



/*
* zu tun: wenn zwei Dateien (oder mehrere?) eingegeben werden, sind die Degrees beim 2. = 0
*
* wenn mehrere Dateien eingegeben werden, zeigt die Fortschrittsbar manchmal 100% an, wenn eine Datei
* fertig ist, aber auch dann wenn noch nicht alle Dateien fertig sind. Idee: jeden Prozess in 
* startNewProcess() in ein Array speichern und bei der Timerausgabe immer alle Prozesse auf ihre
* currentTime checken und dann das maximale nehmen.
*/