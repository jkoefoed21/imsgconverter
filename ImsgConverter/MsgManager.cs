using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;

namespace ImsgConverter
{
    public class MsgManager
    {
        public static readonly string MSGSPath = "E:\\iOSmsgs6-16";

        public static readonly string ABPath = "E:\\iOSAB6-16";

        public static readonly string outPath = "E:\\imsgproj\\";

        private SQLiteConnection msgsDB;

        private SQLiteConnection abDB;

        public static readonly DateTime epoch = new DateTime(2017, 9, 13); //first game was on day one--9/14.

        public MsgManager()
        {
            msgsDB = new SQLiteConnection("Data Source = " + MSGSPath + " ; Version=3");
            msgsDB.Open();
            abDB = new SQLiteConnection("Data Source = " + ABPath + " ; Version=3");
            abDB.Open();
        }

        public static void Main(string[] args)
        {
            MsgManager mm = new MsgManager();
            mm.backupAll();
            //object[][] messages = mm.getData("SELECT text FROM message WHERE handle_id = 0");
            /*Console.WriteLine(messages[0][0]);
            Console.WriteLine(messages[1][0]);
            Console.WriteLine(messages[2][0]);
            Console.WriteLine(messages[3][0]);
            Console.WriteLine(messages[4][0]);*/
            //Console.ReadKey();
        }

        public void backupAll()
        {
            Stack<int> allsenders = new Stack<int>();
            object[][] handles=getData(msgsDB, "SELECT handle_id FROM message WHERE handle_id != 0");
            int currentid = 0;
            for (int ii=0; ii<handles.Length; ii++)
            {
                int id = Int32.Parse(handles[ii][0].ToString());
                if (currentid!= id)
                {
                    allsenders.Push(id);
                }
                currentid = id;
                //Console.ReadKey();
            }
            int[] senders=allsenders.ToArray();
            Dictionary<string, string> contacts = getAB();
            Empty(new DirectoryInfo(outPath));
            for (int ii=0; ii<senders.Length; ii++)
            {
                Console.WriteLine(senders[ii]);
                object[][] msgs = getData(msgsDB, "SELECT * FROM message WHERE handle_id = @val1 AND cache_roomnames IS NULL ORDER BY date ASC", senders[ii], "");
                string[] str = new string[msgs.Length]; 
                for (int jj = 0; jj < msgs.Length; jj++)
                {
                    //Console.WriteLine(msgs.Length);
                    //Console.WriteLine(senders[ii]);
                    string datestr = msgs[jj][15].ToString();
                    datestr=datestr.Substring(0, 9);
                    //Console.WriteLine(datestr);
                    DateTime tstamp = dateFromUnix(Int32.Parse(datestr));
                    tstamp=tstamp.AddHours(-7); //ALL TIMES ARE PDT, NOT PST OR IN LOCAL TIME SENT.
                    string dateout = "" + tstamp.Month + "/" + tstamp.Day + "/" + tstamp.Year + " " + tstamp.Hour + ":" + tstamp.Minute + ":" + tstamp.Second + "PDT";
                    /*Console.WriteLine(tstamp.Year);
                    Console.WriteLine(tstamp.Month);
                    Console.WriteLine(tstamp.Day);
                    Console.WriteLine(tstamp.Hour);
                    Console.WriteLine(tstamp.Minute);
                    Console.WriteLine(tstamp.Second);
                    Console.ReadKey();*/
                    string isfromme = msgs[jj][21].ToString();
                    int me = Int32.Parse(isfromme);
                    string line = me==1 ? "Me: " : "Them: ";
                    line += "\t" + dateout + "\t";
                    line += msgs[jj][2].ToString();
                    str[jj] = line;
                }
                object[][] handleData = getData(msgsDB, "SELECT id FROM handle WHERE ROWID = @val1", senders[ii]);
                string phoneNum = handleData[0][0].ToString();
                string name;
                bool isContact = contacts.TryGetValue(phoneNum, out name);
                if (isContact)
                {
                    phoneNum = name;
                }
                File.WriteAllLines(outPath + phoneNum + ".txt", str);
            }
        }

        public Object[][] getData(SQLiteConnection connect, string query)
        {
            SQLiteDataReader reader = executeOutputQuery(connect, query);
            Queue<Object[]> data = new Queue<Object[]>();
            //Console.WriteLine(reader.)
            while (reader.Read())
            {
                Object[] row = new Object[reader.FieldCount];
                for (int ii = 0; ii < row.Length; ii++)
                {
                    row[ii] = reader[ii];
                }
                data.Enqueue(row);
            }
            return data.ToArray();
        }
        public Object[][] getData(SQLiteConnection connect, string query, object val1)
        {
            SQLiteDataReader reader = executeOutputQuery(connect, query, val1);
            Queue<Object[]> data = new Queue<Object[]>();
            //Console.WriteLine(reader.)
            while (reader.Read())
            {
                Object[] row = new Object[reader.FieldCount];
                for (int ii = 0; ii < row.Length; ii++)
                {
                    row[ii] = reader[ii];
                }
                data.Enqueue(row);
            }
            return data.ToArray();
        }

        public Dictionary<string, string> getAB()
        {
            object[][] addbooknames = getData(abDB, "SELECT ROWID, First, Last FROM ABPerson");
            object[][] addbooknumbers = getData(abDB, "Select record_id, value FROM ABMultiValue");
            Dictionary<string, string> numbers = new Dictionary<string, string>();
            Dictionary<int, string> names = new Dictionary<int, string>();
            for (int ii=0; ii<addbooknames.Length; ii++)
            {
                names.Add(Int32.Parse(addbooknames[ii][0].ToString()), addbooknames[ii][1].ToString() + " " + addbooknames[ii][2].ToString());
            }
            for (int ii=0; ii<addbooknumbers.Length; ii++)
            {
                string name;
                names.TryGetValue(Int32.Parse(addbooknumbers[ii][0].ToString()), out name);
                string number = addbooknumbers[ii][1].ToString();
                if(!number.Contains("@"))
                {
                    while (number.IndexOfAny(new char[] { '(', ')', '-', ' ' })!=-1)
                    {
                        number = number.Remove(number.IndexOfAny(new char[] { '(', ')', '-', (char) 0x20, (char) 0xA0 }), 1);
                    }
                    while (number.IndexOf(" ") != -1)
                    {
                        number.Remove(number.IndexOf(" "), 1);
                    }
                    if(!(number.StartsWith("1")||number.StartsWith("+")))
                    {
                        number = "+1" + number;
                    }
                    if(!number.StartsWith("+"))
                    {
                        number = "+" + number;
                    }
                }
                string test = "";
                if (!numbers.TryGetValue(number, out test)&&!number.Equals(""))
                {
                    /*if (name.Contains("Kirkman"))
                    {
                        Console.WriteLine(number);
                        char[] numChars=number.ToCharArray();
                        foreach (char c in numChars)
                        {
                            Console.WriteLine("{0:X}", (int) c);
                        }   
                        Console.ReadKey();
                    }*/
                    numbers.Add(number, name);
                }
            }
            return numbers;
        }

        public Object[][] getData(SQLiteConnection connect, string query, object val1, object val2)
        {
            SQLiteDataReader reader = executeOutputQuery(connect, query, val1, val2);
            Queue<Object[]> data = new Queue<Object[]>();
            //Console.WriteLine(reader.)
            while (reader.Read())
            {
                Object[] row = new Object[reader.FieldCount];
                for (int ii = 0; ii < row.Length; ii++)
                {
                    row[ii] = reader[ii];
                }
                data.Enqueue(row);
            }
            return data.ToArray();
        }

        public SQLiteDataReader executeOutputQuery(SQLiteConnection connect, string query)
        {
            SQLiteCommand command = new SQLiteCommand(query, connect);
            return command.ExecuteReader();
        }
        public SQLiteDataReader executeOutputQuery(SQLiteConnection connect, string query, object val1)
        {
            SQLiteCommand command = new SQLiteCommand(query, connect);
            command.Parameters.AddWithValue("@val1", val1);
            return command.ExecuteReader();
        }
        public SQLiteDataReader executeOutputQuery(SQLiteConnection connect, string query, object val1, object val2)
        {
            SQLiteCommand command = new SQLiteCommand(query, connect);
            command.Parameters.AddWithValue("@val1", val1);
            command.Parameters.AddWithValue("@val2", val2);
            return command.ExecuteReader();
        }

        public DateTime dateFromUnix(int seconds)
        {
            DateTime epoch = new DateTime(2001, 1, 1, 0, 0, 0, 0);
            return epoch.AddSeconds(seconds);
        }

        public static void Empty(System.IO.DirectoryInfo directory)
        {
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
        }
    }
}
