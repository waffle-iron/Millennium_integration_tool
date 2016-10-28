using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using FirebirdSql.Data.FirebirdClient;
using Dapper;
using System.Runtime.InteropServices;
using System.IO;
using Dapper.Mapper;
using System.IO.Compression;
using NLog;
using Dicom;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using Point = System.Windows.Point;
using Dicom.Imaging;
using Dicom.Imaging.LUT;
using Dicom.Imaging.Render;
using Dicom.Imaging.Mathematics;
using System.Globalization;
using System.Security.Permissions;
using System.Threading;
using Dicom.IO;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;

using NLog.Targets;
using NLog.Config;
//using NLog.Win32.Targets;




namespace Mconverter
{
    static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GetDllDirectory(int bufsize, StringBuilder buf);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);
    }



    class Program
    {

        #region Private Fields
       
        private readonly List<string> _listModality = new List<string> { "R-CC", "L-CC", "R-MLO", "L-MLO" };
        private Logger _log = LogManager.GetLogger("Program");
        #endregion


        //string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void Improcessing([MarshalAs(UnmanagedType.LPStr)] string PathFrom, int ImageType, [MarshalAs(UnmanagedType.LPStr)] string fn);




        static void Main(string[] args)
        {


            // const int FT = 0;

            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            Console.WriteLine(path);
            string subpath = @"exportdata";
            string subpath1 = @"importdicom";
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
            dirInfo.CreateSubdirectory(subpath);
            dirInfo.CreateSubdirectory(subpath1);


            string libpath = path + "\\";
            Console.WriteLine(libpath);
            NativeMethods.SetDllDirectory(path);

          


            IntPtr pDll3 = NativeMethods.LoadLibrary("Pimg.dll");
            //error handling here
            if (pDll3 == IntPtr.Zero)
                throw new DllNotFoundException("Dll not found");

            // Step 1. Create configuration object

            LoggingConfiguration config = new LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration

            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);

            FileTarget fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            // Step 3. Set target properties

            consoleTarget.Layout = "${date:format=HH\\:MM\\:ss} ${logger} ${message}";
            fileTarget.FileName = "${basedir}/file.txt";
            fileTarget.Layout = "${message}";

            // Step 4. Define rules

            LoggingRule rule1 = new LoggingRule("*", LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(rule1);

            LoggingRule rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
            config.LoggingRules.Add(rule2);

            // Step 5. Activate the configuration

            LogManager.Configuration = config;

            // Example usage

            Logger logger = LogManager.GetLogger("Program");
            /*logger.Trace("trace log message");
            logger.Debug("debug log message");
            logger.Info("info log message");
            logger.Warn("warn log message");
            logger.Error("error log message");
            logger.Fatal("fatal log message");
        
            */
            

            // strConnectionString s- соединение с базой ARHIMED
            //string strConnectionString = System.Configuration.ConfigurationManager.AppSettings["DB_CONN_STRING"];
            string strConnectionString2 = System.Configuration.ConfigurationManager.AppSettings["DB_CONN_STRING2"];
            //string strConnectionStringM = System.Configuration.ConfigurationManager.AppSettings["MSSQL_CONN_STRING"];


            // SqlConnection myConnection = new SqlConnection(strConnectionStringM);


            Console.WriteLine(strConnectionString2);
            var paths = new List<string>(ConfigurationManager.AppSettings["DB_CONN_STRING2"].Split(new char[] { ';' }));
            //paths.ForEach(Console.WriteLine);
            //Console.WriteLine(paths[3]);
            var Databases = new List<string>(paths[3].Split(new char[] { '=' }));
            Databases.ForEach(Console.WriteLine);
            var Usernames = new List<string>(paths[1].Split(new char[] { '=' }));
            string User_Name = Usernames[1];
            string Password = paths[2];
            string Database = Databases[1];

            Console.WriteLine(User_Name);

            string[] imagepaths = Directory.GetFiles(Database, "Im*.fdb");
            Console.WriteLine("The number image databases  is {0}.", imagepaths.Length);

            foreach (string imagepath in imagepaths)
            {
                string strConnectionStringI = paths[0] + ';' + paths[1] + ';' + paths[2] + "; Database=" + imagepath;
                Console.WriteLine(strConnectionStringI);
            }

            
            foreach (string imagepath in imagepaths)
            {
                string strConnectionStringI = paths[0] + ';' + paths[1] + ';' + paths[2] + "; Database=" + imagepath;
                Console.WriteLine(strConnectionStringI);
                //Console.ReadLine();
                //FbConnection db = new FbConnection(strConnectionString);
                FbConnection db2 = new FbConnection(strConnectionStringI);
                
                db2.Open();
               
                // db.Open();
                


                //ISOBR
                string SqlString2 = "SELECT   ID, NUMBERKART,   ISOBR FROM ISOBR";
               
                var ourisobr = (IEnumerable<isobr>)db2.Query<isobr>(SqlString2, buffered: false);
                foreach (var isobr in ourisobr)
                {

                    string filename1 = "id" + isobr.ID.ToString() + "NK" + isobr.NUMBERKART.ToString() + ".dcm";
                    Console.WriteLine(filename1);
                    string filename = path + "\\exportdata\\" + isobr.ID.ToString() + "NK" + isobr.NUMBERKART.ToString() + ".dcm";
                    // Console.WriteLine(filename);
                    int length = isobr.ISOBR.Length;
                    long numberk = isobr.NUMBERKART;



                    IntPtr pAddressOfFunctionToCall3 = NativeMethods.GetProcAddress(pDll3, "Improcessing");
                    //oh dear, error handling here
                    if (pAddressOfFunctionToCall3 == IntPtr.Zero)
                        throw new DllNotFoundException("Dll does not contain required function");

                    Improcessing imP = (Improcessing)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall3, typeof(Improcessing));

                    byte[] theResult = isobr.ISOBR;
                    RetrieveStandardImage(filename, theResult);

                    imP(filename, 0, filename1);

                    // Directory.Delete(path + "\\exportdata\\", true);

                    // bool result = NativeMethods.FreeLibrary(pDll3);

                }
                //db2.Close();
               //Directory.Delete(path + "\\exportdata\\", true);
             }
            bool result = NativeMethods.FreeLibrary(pDll3);
            var samplesDir = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory), "exportdata", "dicom");
            var testDir = Path.Combine(samplesDir, "Test");
            if (!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);

            try
            {


                if (!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);
                // Only get files that .dcm"
                string[] dirs = Directory.GetFiles(@"c:\exportdicom\", "*.DCM");
                Console.WriteLine("The number of  dicom files  is {0}.", dirs.Length);

                foreach (string dir in dirs)
                {
                    string fname = Path.GetFileNameWithoutExtension(dir);

                    Char charRange = 'N';

                    int start = fname.IndexOf(charRange);
                    string r = fname.Substring(start + 2);
                     Console.WriteLine(r);

                    
                    var df = DicomFile.Open(dir);

                    Console.WriteLine(df.ToString());
                    //Console.ReadLine();
                    Save(df, r);
                   


                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }

        }

        


        /// <summary>
        /// Strip RichTextFormat from the string
        /// </summary>
        /// <param name="rtfString">The string to strip RTF from</param>
        /// <returns>The string without RTF</returns>
        public static string StripRTF(string rtfString)
        {
            string result = rtfString;

            try
            {
                if (IsRichText(rtfString))
                {
                    // Put body into a RichTextBox so we can strip RTF
                    using (System.Windows.Forms.RichTextBox rtfTemp = new System.Windows.Forms.RichTextBox())
                    {
                        rtfTemp.Rtf = rtfString;
                        result = rtfTemp.Text;
                    }
                }
                else
                {
                    result = rtfString;
                }
            }
            catch
            {
                throw;
            }

            return result;
        }



        /// <summary>
        /// Checks testString for RichTextFormat
        /// </summary>
        /// <param name="testString">The string to check</param>
        /// <returns>True if testString is in RichTextFormat</returns>
        public static bool IsRichText(string testString)
        {
            if ((testString != null) &&
                (testString.Trim().StartsWith("{\\rtf")))
            {
                return true;
            }
            else
            {
                return false;
            }
        }




        /// <summary>
        /// RetrieveStandardImage
        /// </summary>
        /// <param name="_fname"></param>
        /// <param name="_blob"></param>
        /// <returns></returns>
        public static bool RetrieveStandardImage(string _fname, byte[] _blob)
        {

            try
            {
                //Create a new filestream
                //Replace the path with the desired location where you want the files to be written
                FileStream _fs = new FileStream(_fname, FileMode.Create, FileAccess.Write);


                //Writing the stream to file
                _fs.Write(_blob, 0, _blob.Length);

                //Closing the stream
                _fs.Close();

                //If it succeeds
                return true;
            }
            catch (Exception e)
            {
                //Any errors will be outputted to the console
                Console.WriteLine(e.ToString());
            }
            //If there's a problem, i.e. it fails
            return false;
        }




        public static void Save(DicomFile fil, string filn)
        {


            string strConnectionStringM = System.Configuration.ConfigurationManager.AppSettings["MSSQL_CONN_STRING"];
            string strConnectionString = System.Configuration.ConfigurationManager.AppSettings["DB_CONN_STRING"];
            FbConnection db = new FbConnection(strConnectionString);

           Console.WriteLine(strConnectionString);


          


            DicomFile _dicomFile = null;
            DicomDataset _dicomDataset = null;

            var dcf = fil;
            Console.WriteLine(filn);
            

            using (var memStream = new MemoryStream())
            {
                _dicomFile = dcf;
                _dicomDataset = dcf.Dataset;
                Console.WriteLine(dcf);
                string _patientID = _dicomDataset.Get<string>(DicomTag.PatientID);
                string _studyID = _dicomDataset.Get<string>(DicomTag.StudyID);
                string _patientSex = _dicomDataset.Get<string>(DicomTag.PatientSex);
                Console.WriteLine(_patientID);
                string _studyInstanceUID = _dicomDataset.Get<string>(DicomTag.StudyInstanceUID);
                Console.WriteLine(_dicomDataset.Get<string>(DicomTag.StudyInstanceUID));
                string _seriesInstanceUID = _dicomDataset.Get<string>(DicomTag.SeriesInstanceUID);

                string _modality = _dicomDataset.Get<string>(DicomTag.Modality);
                Console.WriteLine(_modality);
                string _seriesDescription = _dicomDataset.Get<string>(DicomTag.SeriesDescription);
                string _patientName = _dicomDataset.Get<string>(DicomTag.PatientName).Replace('^', ' ');
                Console.WriteLine(_patientName);
                DateTime _personBirthDate = _dicomDataset.Get<DateTime>(DicomTag.PatientBirthDate);
                Console.WriteLine(_personBirthDate);
                string _comments = _dicomDataset.Get<string>(DicomTag.PatientComments);
                Console.WriteLine(_comments);
                Guid _id = Guid.NewGuid();
                Guid _persid = Guid.NewGuid();
                Guid _aeTitleId = Guid.NewGuid();
                DateTime dateTime = _dicomDataset.Get<DateTime>(DicomTag.StudyDate);
                Console.WriteLine(dateTime);




                _dicomFile.Save(memStream);

                using (SqlConnection myC = new SqlConnection(strConnectionStringM))

                {

                    string vsql4 = string.Format("Insert Into Millennium.dbo.ApplicationEntity" + "(ID, AeTitle, Description) Values (@ID, @AeTitle, @Description)");
                    string vsql1 = string.Format("IF NOT EXISTS(Select * from Millennium.dbo.Studies  where StudyInstanceUID = @StudyInstanceUID)" + " Insert Into Millennium.dbo.Studies" + "(ID, Date, PersonID, Note , Result, MeanDose, Dose, iheClassEHRCode, Prophylaxis,  IsResulted,IsPrinted, X_rayProcedureID, AeTitleId, StudyInstanceUID, Comment) Values (@ID, @Date, @PersonID, @Note, @Result, @MeanDose, @Dose, @iheClassEHRCode, @Prophylaxis, @IsResulted, @IsPrinted, @X_rayProcedureID, @AeTitleId,  @StudyInstanceUID, @Comment)" + "ELSE Update Millennium.dbo.Studies set Date = @Date  where StudyInstanceUID = @StudyInstanceUID");
                    string vsql3 = string.Format("IF NOT EXISTS(Select ID from Millennium.dbo.Studies  where StudyInstanceUID = @StudyInstanceUID)" + "Insert Into Millennium.dbo.Person" + "(ID, Name, Birthday, iheGenderCode) Values (@ID, @Name, @Birthday,@iheGenderCode)" + "ELSE Update Millennium.dbo.Person set Birthday = @Birthday, Name = @Name where ID = (Select PersonID from Millennium.dbo.Studies  where StudyInstanceUID = @StudyInstanceUID)");
                    string vsql2 = "IF NOT EXISTS(Select *  from Millennium.dbo.Studies  INNER JOIN Millennium.dbo.StudiesData ON Millennium.dbo.Studies.ID = Millennium.dbo.StudiesData.StudiesID where Millennium.dbo.Studies.StudyInstanceUID= @StudyInstanceUID)"
                                    + "Insert Into Millennium.dbo.StudiesData"
                                    + "(ID,StudiesID, FileId, Data, TransferSyntaxUID, Invert,  SeriesInstanceUID, StudyInstanceUID, InstanceNumber) Values (@ID,@StudiesID, @FileId, @Data, @TransferSyntaxUID, @Invert, @SeriesInstanceUID, @StudyInstanceUID, @InstanceNumber)"
                                    + "ELSE Insert Into Millennium.dbo.StudiesData"
                                    + "(ID,StudiesID, FileId, Data, TransferSyntaxUID, Invert,  SeriesInstanceUID, StudyInstanceUID, InstanceNumber) Values (@ID, (Select ID from Millennium.dbo.Studies where StudyInstanceUID = @StudyInstanceUID), @FileId, @Data, @TransferSyntaxUID, @Invert, @SeriesInstanceUID, @StudyInstanceUID, @InstanceNumber)";
                    SqlCommand cmd3 = new SqlCommand(vsql4, myC);
                    SqlCommand cmd2 = new SqlCommand(vsql3, myC);
                    SqlCommand cmd1 = new SqlCommand(vsql1, myC);
                    SqlCommand cmd = new SqlCommand(vsql2, myC);


                    cmd2.Parameters.Add("@StudyInstanceUID", SqlDbType.NVarChar).Value = _studyInstanceUID;
                    cmd3.Parameters.Add("@ID", SqlDbType.UniqueIdentifier).Value = _aeTitleId;
                    cmd3.Parameters.Add("@AeTitle", SqlDbType.NVarChar).Value = "ARCHIMED";
                    cmd3.Parameters.Add("@Description", SqlDbType.NVarChar).Value = "Создан";
                    cmd2.Parameters.Add("@ID", SqlDbType.UniqueIdentifier).Value = _persid;
                    //Kart_Pac

                    try
                    {
                        db.Open();
                        string SqlString3 = "SELECT  FIO FROM KART_PAC WHERE NUMBERKART = " + filn;
                        var ourkart_pac = (List<kart_pac>)db.Query<kart_pac>(SqlString3);
                       Console.WriteLine(ourkart_pac.ElementAt(0).FIO);
                      
                        if (ourkart_pac != null)
                        {
                            if (ourkart_pac.ElementAt(0).FIO != null)
                            {
                                string fiio = ourkart_pac.ElementAt(0).FIO;
                                
                                Console.WriteLine(fiio);
                                cmd2.Parameters.Add("@Name", SqlDbType.NVarChar).Value = fiio;


                            }
                            else
                            {
                                cmd2.Parameters.Add("@Name", SqlDbType.NVarChar).Value = _patientName;

                            }
                        }



                        else
                        {
                            Console.WriteLine("***************");
                        }
                        db.Close();
                    }


                    catch (Exception e)
                    {
                        Console.WriteLine("нет соотоветствия в базе CLD, plain Dicom");
                        cmd2.Parameters.Add("@Name", SqlDbType.NVarChar).Value = _patientName;
                        db.Close();
                        // Console.WriteLine("The process failed: {0}", e.ToString());
                        //logger.Warn(e);
                    }
                    

                    try
                    {
                        db.Open();
                        string SqlString4 = "SELECT NUMBERKART, OPISANIE, SAKL FROM PROTOCOL WHERE NUMBERKART = " + filn;
                        var out_protocol = (IEnumerable<protocol>)db.Query<protocol>(SqlString4);

                        if (out_protocol != null)
                        {
                            
                            if (out_protocol.ElementAt(1).OPISANIE != null)
                            {
                                string protoc = StripRTF(out_protocol.ElementAt(1).OPISANIE);
                                cmd1.Parameters.Add("@Note", SqlDbType.NVarChar).Value = protoc;
                            
                                Console.WriteLine(protoc);
                            }
                            else
                            {
                                cmd1.Parameters.Add("@Note", SqlDbType.NVarChar).Value = "  ";
                            }

                            
                        }

                        
                        else
                        {
                            Console.WriteLine("************");
                           
                        }
                        db.Close();
                    }


                    catch (Exception e)
                    {
                        Console.WriteLine("нет соотоветствующей базы CLD, файлы загружаются без протоколов");
                        cmd1.Parameters.Add("@Note", SqlDbType.NVarChar).Value = "  ";
                        db.Close();
                        // Console.WriteLine("The process failed: {0}", e.ToString());
                        //logger.Warn(e);
                    }






                    // birthday and null excepеtion
                    DateTime etalon = new DateTime(1753, 1, 1, 12, 0, 0);
                        DateTime nothing = new DateTime();
                        Console.WriteLine(etalon);

                        if (_personBirthDate != nothing && _personBirthDate >= etalon)
                        {
                            cmd2.Parameters.Add("@Birthday", SqlDbType.DateTime).Value = _personBirthDate;
                        }
                        else
                        {
                            cmd2.Parameters.Add("@Birthday", SqlDbType.DateTime).Value = dateTime;
                        }

                        // patient sex
                        if (_patientSex != null)
                        {
                            if (_patientSex.Equals("M"))
                            {
                                cmd2.Parameters.Add("@iheGenderCode", SqlDbType.TinyInt).Value = 1;
                            }
                            else if (_patientSex.Equals("F"))
                            {
                                cmd2.Parameters.Add("@iheGenderCode", SqlDbType.TinyInt).Value = 2;
                            }
                            else
                            {
                                cmd2.Parameters.Add("@iheGenderCode", SqlDbType.TinyInt).Value = 3;
                            }
                        }
                        else
                        {
                            cmd2.Parameters.Add("@iheGenderCode", SqlDbType.TinyInt).Value = 3;
                        }



                    cmd1.Parameters.Add("@ID", SqlDbType.UniqueIdentifier).Value = _id;
                    cmd1.Parameters.Add("@Date", SqlDbType.DateTime).Value = dateTime;
                    cmd1.Parameters.Add("@PersonID", SqlDbType.UniqueIdentifier).Value = _persid;
                   // cmd1.Parameters.Add("@Note", SqlDbType.NVarChar).Value = "protoc";
                 
                    /*
               
                        if (out_protocol.ElementAt(1).OPISANIE != null)
                        {
                            string protoc = StripRTF(out_protocol.ElementAt(1).OPISANIE);
                            cmd1.Parameters.Add("@Note", SqlDbType.NVarChar).Value = protoc;
                        Console.WriteLine(protoc);
                         }
                        else 
                       {
                        Console.WriteLine(" нет протокола");
                    }*/

                    cmd1.Parameters.Add("@Result", SqlDbType.NVarChar).Value = " ";

                 /*
                    if (out_protocol.ElementAt(0).SAKL != null)
                    {
                        string sakl = StripRTF(out_protocol.ElementAt(0).SAKL);
                    Console.WriteLine(sakl);
                    cmd1.Parameters.Add("@Result", SqlDbType.NVarChar).Value = sakl;
                    }
                    .else 
                {
                     cmd1.Parameters.Add("@Result", SqlDbType.NVarChar).Value = null;
                    Console.WriteLine("no sakl");
                    }*/

                    cmd1.Parameters.Add("@IsResulted", SqlDbType.Bit).Value = 0;
                    cmd1.Parameters.Add("@Prophylaxis", SqlDbType.Bit).Value = 0;
                    cmd1.Parameters.Add("@MeanDose", SqlDbType.Decimal).Value = 0.0;
                    cmd1.Parameters.Add("@Dose", SqlDbType.Decimal).Value = 0.0;
                    cmd1.Parameters.Add("@iheClassEHRCode", SqlDbType.NVarChar).Value = 2;
                    cmd1.Parameters.Add("@IsPrinted", SqlDbType.Bit).Value = 0;


                    // modality
                    if (_modality.Equals("CR"))
                        {
                            cmd1.Parameters.Add("@X_rayProcedureID", SqlDbType.TinyInt).Value = 4;
                        }
                        else if (_modality.Equals("CT"))
                        {
                            cmd1.Parameters.Add("@X_rayProcedureID", SqlDbType.TinyInt).Value = 6;
                        }
                        else if (_modality.Equals("MG"))
                        {
                            cmd1.Parameters.Add("@X_rayProcedureID", SqlDbType.TinyInt).Value = 4;
                        }
                        else if (_modality.Equals("MR"))
                        {
                            cmd1.Parameters.Add("@X_rayProcedureID", SqlDbType.TinyInt).Value = 9;
                        }
                        else if (_modality.Equals("US"))
                        {
                            cmd1.Parameters.Add("@X_rayProcedureID", SqlDbType.TinyInt).Value = 10;
                        }
                        else if (_modality.Equals("RF"))
                        {
                            cmd1.Parameters.Add("@X_rayProcedureID", SqlDbType.TinyInt).Value = 1;
                        }
                        else if (_modality.Equals("DF"))
                        {
                            cmd1.Parameters.Add("@X_rayProcedureID", SqlDbType.TinyInt).Value = 2;
                        }
                        else
                        {
                            cmd1.Parameters.Add("@X_rayProcedureID", SqlDbType.TinyInt).Value = 8;

                        }

                        // Studies
                        cmd1.Parameters.Add("@AeTitleId", SqlDbType.UniqueIdentifier).Value = _aeTitleId;
                        cmd1.Parameters.Add("@StudyInstanceUID", SqlDbType.NVarChar).Value = _dicomDataset.Get<string>(DicomTag.StudyInstanceUID);

                        // Comment
                        if (_comments != null)
                        {
                            cmd1.Parameters.Add("@Comment", SqlDbType.NVarChar).Value = _comments;
                        }
                        else
                        {
                            cmd1.Parameters.Add("@Comment", SqlDbType.NVarChar).Value = "1";
                        }


                        // studies_data
                        cmd.Parameters.Add("@ID", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                        cmd.Parameters.Add("@data", SqlDbType.VarBinary).Value = memStream.GetBuffer();
                        cmd.Parameters.Add("@invert", SqlDbType.Bit).Value = 0;
                        cmd.Parameters.Add("@transferSyntaxUID", SqlDbType.NVarChar).Value = _dicomDataset.InternalTransferSyntax.ToString();

                        // cmd.Parameters.Add("@windowsWidth", SqlDbType.Decimal).Value = dicomImage.WindowWidth;
                        // cmd.Parameters.Add("@windowsCenter", SqlDbType.Decimal).Value = dicomImage.WindowCenter;
                        cmd.Parameters.Add("@studiesID", SqlDbType.UniqueIdentifier).Value = _id;
                        cmd.Parameters.Add("@fileId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                        cmd.Parameters.Add("@SeriesInstanceUID", SqlDbType.NVarChar).Value = _dicomDataset.Get<string>(DicomTag.SeriesInstanceUID);
                        cmd.Parameters.Add("@StudyInstanceUID", SqlDbType.NVarChar).Value = _dicomDataset.Get<string>(DicomTag.StudyInstanceUID);
                        cmd.Parameters.Add("@InstanceNumber", SqlDbType.SmallInt).Value = _dicomDataset.Get<string>(DicomTag.InstanceNumber);



                        myC.Open();
                        cmd3.ExecuteNonQuery();
                        cmd3 = null;
                        cmd2.ExecuteNonQuery();
                        cmd2 = null;
                        cmd1.ExecuteNonQuery();
                        cmd1 = null;
                        cmd.ExecuteNonQuery();
                        cmd = null;
                        myC.Close();
                  

                }



                }

                
            }

        }
    }











