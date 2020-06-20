using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace column_generation
{
    class read_file
    {
        public read_file(string input_file_str)
        {
            this.input_file_str = input_file_str;
            departure_time_range = read_dep_time_range();
            running_time = train_running_time();
            total_train_num = running_time.Rows.Count;
            try
            {
                dir = direction();
                is_double = false;
            }
            catch (Exception)
            {
                dir = new int[total_train_num];
                for (int i = 0; i < total_train_num; i++)
                {
                    dir[i] = 1;
                }
                is_double = true;
            }
            blocking_time = train_blocking_time();
            max_waiting_time = max_wait_time();
            min_waiting_time = min_wait_time();
            stop_seq = sequence_of_stop();
            time_information(out add_start, out add_stop, out start_time, out end_time);
            time_len = time_sub2min(end_time, start_time) + 1;
            trian_type = train_type(out agent_type);
            zone = network_zone();
            station_num = running_time.Columns.Count;
        }
        public bool is_double;
        string input_file_str;
        public DataTable departure_time_range;
        public int[] dir;
        public int total_train_num;
        public int time_len;
        public DataTable blocking_time;
        public DataTable max_waiting_time;
        public DataTable min_waiting_time;
        public List<int>[] stop_seq;
        public int add_start;
        public int add_stop;
        public int start_time;
        public int end_time;
        public DataTable running_time;
        public Dictionary<int, string> trian_type;
        public DataTable zone;
        public DataTable agent_type;
        public int station_num;

        private DataTable define_agent_type()
        {
            DataTable agent_type = new DataTable();
            DataColumn agent_type_ = new DataColumn("agent_type", typeof(string));
            DataColumn name = new DataColumn("name", typeof(string));
            agent_type.Columns.Add(agent_type_); agent_type.Columns.Add(name);

            return agent_type;
        }
        private DataTable read(string str)
        {
            FileStream fs = new FileStream(str, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            string line = sr.ReadLine();
            string[] headers = line.Split(',');
            DataTable dt = new DataTable();
            foreach (var h in headers)
            {
                DataColumn dc = new DataColumn(h, typeof(string));
                dt.Columns.Add(dc);
            }
            while ((line = sr.ReadLine()) != null)
            {
                DataRow dr = dt.NewRow();
                string[] obj = line.Split(',');
                for (int i = 0; i < obj.Length; i++)
                {
                    dr[i] = obj[i];
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }
        private DataTable read_dep_time_range()
        {
            string path = input_file_str + "\\departure time range.csv";
            return read(path);
        }
        private int[] direction()
        {
            string path = input_file_str + "\\direction.csv";
            DataTable dir = read(path);
            int[] direction = new int[dir.Rows.Count];
            for (int i = 0; i < dir.Rows.Count; i++)
            {
                direction[i] = int.Parse((string)dir.Rows[i][1]);
            }
            return direction;
        }
        private DataTable train_blocking_time()
        {
            string path = input_file_str + "\\blocking time.csv";
            return read(path);
        }
        private DataTable max_wait_time()
        {
            string path = input_file_str + "\\max waiting time.csv";
            return read(path);
        }
        private DataTable min_wait_time()
        {
            string path = input_file_str + "\\min waiting time.csv";
            return read(path);
        }
        private List<int>[] sequence_of_stop()
        {
            string path = input_file_str + "\\sequence of stops.csv";
            System.Data.DataTable seq = read(path);
            List<int>[] stop_seq = new List<int>[seq.Rows.Count];
            for (int i = 0; i < seq.Rows.Count; i++)
            {
                string stations = (string)seq.Rows[i][1];
                string[] station_list = stations.Split(';');
                List<int> now_stop_seq = new List<int>();
                for (int ii = 0; ii < station_list.Length; ii++)
                {
                    if (station_list[ii] != "")
                    {
                        now_stop_seq.Add(int.Parse(station_list[ii]));
                    }
                }
                stop_seq[i] = now_stop_seq;
            }
            return stop_seq;
        }
        private void time_information(out int add_start, out int add_stop, out int start_time, out int end_time)
        {
            string path = input_file_str + "\\time information.csv";
            DataTable dt = read(path);
            add_start = int.Parse((string)dt.Rows[0][0]);
            add_stop = int.Parse((string)dt.Rows[0][1]);
            start_time = int.Parse((string)dt.Rows[0][2]);
            end_time = int.Parse((string)dt.Rows[0][3]);
        }
        private DataTable train_running_time()
        {
            string path = input_file_str + "\\train running time.csv";
            return read(path);
        }
        private Dictionary<int, string> train_type(out DataTable agent_type)
        {
            agent_type = new DataTable();
            DataColumn agent_type_ = new DataColumn("agent_type", typeof(string));
            DataColumn name = new DataColumn("name", typeof(string));
            agent_type.Columns.Add(agent_type_); agent_type.Columns.Add(name);
            Dictionary<int, string> train_type = new Dictionary<int, string>();
            string path = input_file_str + "\\train type.csv";
            DataTable dt = read(path);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = agent_type.NewRow();
                dr[0] = (string)dt.Rows[i][0];
                string[] train_list = ((string)dt.Rows[i][1]).Split(';');
                for (int j = 0; j < train_list.Length; j++)
                {
                    int now_trian = int.Parse(train_list[j]);
                    train_type.Add(now_trian, (string)dt.Rows[i][0]);
                }
                agent_type.Rows.Add(dr);
            }
            return train_type;
        }
        private DataTable network_zone()
        {
            string path = input_file_str + "\\zone.csv";
            return read(path);
        }
        private int time_sub2min(int t1, int t2)
        {
            int h1 = t1 / 100;
            int m1 = t1 % 100;
            int h2 = t2 / 100;
            int m2 = t2 % 100;
            return (h1 - h2) * 60 + m1 - m2;
        }
    }
}
