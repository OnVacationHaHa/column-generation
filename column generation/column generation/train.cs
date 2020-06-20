using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace column_generation
{
    class train
    {
        read_file r;
        int total_num;
        int train_id;
        public List<int> station_range;
        int tf, tp;
        public train(read_file r, int train_id)
        {
            tf = int.Parse((string)r.blocking_time.Rows[train_id - 1][1]);
            tp = int.Parse((string)r.blocking_time.Rows[train_id - 1][2]);
            this.r = r;
            total_num = ((r.station_num - 2) * 3 + 2) * r.time_len + 2;// with start point and end point
            this.train_id = train_id;
            station_range = new List<int>();
            if (r.dir[train_id - 1] == 1)
            {
                for (int i = 1; i <= r.station_num; i++)
                {
                    station_range.Add(i);
                }
            }
            else
            {
                for (int i = r.station_num; i >= r.station_num; i--)
                {
                    station_range.Add(i);
                }
            }
        }
        private int[] Dijkstra(int 起点, int 终点, double[,] network)
        {
            int i = 起点;
            int 列数 = network.GetLength(1);
            List<int> list = new List<int>();
            for (int ii = 1; ii <= 列数; ii++)
            {
                list.Add(ii);
            }
            int[] pred = new int[列数];
            for (int ii = 0; ii < 列数; ii++)
            {
                pred[ii] = 起点;
            }
            double[] d = new double[列数];
            for (int ii = 0; ii < 列数; ii++)
            {
                d[ii] = double.PositiveInfinity;
            };
            d[起点 - 1] = 0;
            pred[起点 - 1] = 起点;
            list.RemoveAt(i - 1);
            while (list.Count != 0)
            {
                for (int k = 1; k <= list.Count; k++)
                {
                    int j = list[k - 1];
                    if (d[j - 1] > d[i - 1] + network[i - 1, j - 1])
                    {
                        d[j - 1] = d[i - 1] + network[i - 1, j - 1];
                        pred[j - 1] = i;
                    }
                }
                double[] d_temp = new double[list.Count];
                for (int ii = 1; ii <= list.Count; ii++)
                {
                    d_temp[ii - 1] = d[list[ii - 1] - 1];
                }
                int index = 0;
                for (int ii = 0; ii < d_temp.Length; ii++)
                {
                    if (d_temp[index] > d_temp[ii])
                    {
                        index = ii;
                    }
                }
                i = list[index];
                list.RemoveAt(index);
            }
            int[] path = new int[1] { 0 };
            if (d[终点 - 1] != double.PositiveInfinity)
            {
                path[0] = 终点;
                int now_node = 终点;
                while (now_node != 起点)
                {
                    double pre_node = pred[now_node - 1];
                    int[] aaa = new int[1] { (int)pre_node };
                    path = 合并数组(aaa, path);
                    now_node = (int)pre_node;
                }
                path = del(path);
            }
            return path;
        }
        public int[,] column(int[] path)
        {
            int[,] col = new int[(r.station_num - 1) * 2, r.time_len];
            for (int i = 1; i <= path.Length; i++)
            {
                int[] sst = node_num2sst(path[i - 1]);
                for (int t = (sst[2] - tf > 1 ? sst[2] - tf : 1); t < (sst[2] + tp <= r.time_len ? sst[2] + tp : r.time_len); t++)
                {
                    if (sst[0] == 1)
                    {
                        col[0, t - 1] = 1;
                    }
                    else if (sst[0] == r.station_num)
                    {
                        col[(r.station_num - 1) * 2 - 1, t - 1] = 1;
                    }
                    else
                    {
                        if (sst[1] == 1)
                        {
                            col[(sst[0] - 2) * 2 + 2 - 1, t - 1] = 1;
                        }
                        else if (sst[1] == 2)
                        {
                            col[(sst[0] - 2) * 2 + 3 - 1, t - 1] = 1;
                        }
                        else
                        {
                            col[(sst[0] - 2) * 2 + 2 - 1, t - 1] = 1;
                            col[(sst[0] - 2) * 2 + 3 - 1, t - 1] = 1;
                        }
                    }
                }
            }
            return col;
        }
        public int cost(int[] path)
        {
            int[] sst1 = node_num2sst(path[0]);
            int[] sst2 = node_num2sst(path.Last());
            return sst2[2] - sst1[2];
        }
        private double[,] init_network_with_miu(double[,] miu)
        {
            double[,] network = new double[total_num, total_num];//1-start_point,2-end_point
            for (int i = 0; i < total_num; i++)
            {
                for (int j = 0; j < total_num; j++)
                {
                    if (i == j)
                        continue;
                    network[i, j] = double.PositiveInfinity;
                }
            }
            //连接逻辑起点
            for (int now_time = 1; now_time <= r.time_len; now_time++)
            {
                int now_node = sst2node_num(station_range[0], 2, now_time);
                network[0, now_node - 1] = now_time;
                for (int tt = now_time - tf; tt <= now_time + tp - 1; tt++)
                {
                    if (tt < 1 || tt > r.time_len)
                        continue;
                    network[0, now_node - 1] -= miu[0, tt - 1];
                }
            }
            //连接逻辑终点
            for (int now_time = 1; now_time <= r.time_len; now_time++)
            {
                int now_node = sst2node_num(station_range.Last(), 1, now_time);
                network[now_node - 1, 1] = 0;
            }
            //连接所有站间
            for (int i = 1; i <= station_range.Count - 1; i++)
            {
                int now_station = station_range[i - 1];
                int next_station = station_range[i];
                int running_time;
                if (station_range[0] == 1)
                {
                    running_time = int.Parse((string)r.running_time.Rows[train_id - 1][i]);
                }
                else

                {
                    running_time = int.Parse((string)r.running_time.Rows[train_id - 1][station_range.Count - i]);
                }
                bool now_stop = false, next_stop = false;
                if (r.stop_seq[train_id - 1].Exists(t => t == now_station))
                {
                    now_stop = true;
                }
                if (r.stop_seq[train_id - 1].Exists(t => t == next_station))
                {
                    next_stop = true;
                }
                link_station(network, now_station, next_station, running_time, now_stop, next_stop, miu);
            }
            //连接所有站内
            for (int i = 2; i <= station_range.Count - 1; i++)
            {
                int now_station = station_range[i - 1];
                for (int now_time = 1; now_time <= r.time_len; now_time++)
                {
                    int min_wait_time = int.Parse((string)r.min_waiting_time.Rows[train_id - 1][i - 1]);
                    if (min_wait_time == 0)
                        min_wait_time = 1;
                    int max_wait_time = int.Parse((string)r.max_waiting_time.Rows[train_id - 1][i - 1]);
                    int now_node = sst2node_num(now_station, 1, now_time);
                    for (int next_time = now_time + min_wait_time; next_time <= (now_time + max_wait_time < r.time_len ? now_time + max_wait_time : r.time_len); next_time++)
                    {
                        int next_node = sst2node_num(now_station, 2, next_time);
                        network[now_node - 1, next_node - 1] = next_time - now_time;
                        for (int tt = next_time - tf; tt < next_time + tp; tt++)
                        {
                            if (tt < 1 || tt > r.time_len)
                            {
                                continue;
                            }
                            int ii = (now_station - 2) * 2 + 3;
                            network[now_node - 1, next_node - 1] -= miu[ii - 1, tt - 1];
                        }
                    }
                }
            }
            return network;
        }
        public int[] get_path(double[,] miu)
        {
            double[,] network = init_network_with_miu(miu);
            return Dijkstra(1, 2, network);
        }
        public int[] format_change(List<int[]> path)
        {
            List<int> path_ = new List<int>();
            int now_station = 1;
            int dep_time = path[0][1];int arr_time;
            int node = sst2node_num(now_station++, 2, dep_time);
            path_.Add(node);
            for (int i = 1; i < path.Count-1; i++)
            {
                arr_time = path[i][0];
                dep_time = path[i][1];
                if (arr_time != dep_time)
                {
                    int node1 = sst2node_num(now_station, 1, arr_time);
                    int node2 = sst2node_num(now_station++, 2, dep_time);
                    path_.Add(node1); path_.Add(node2);
                }
                else
                {
                    node = sst2node_num(now_station++, 3, arr_time);
                    path_.Add(node);
                }
            }
            arr_time = path.Last()[0];
            node = sst2node_num(now_station++, 1, arr_time);
            path_.Add(node);
            int[] path__ = new int[path_.Count];
            for (int i = 0; i < path__.Length; i++)
            {
                path__[i] = path_[i];
            }
            return path__;
        }
        public void generate_nexta(List<int[]> path_, int train_id, DataTable node, DataTable road_link, DataTable agent, ref int road_link_id)
        {
            int[] path = format_change(path_);
            DataRow now_agent = agent.NewRow();
            now_agent[0] = train_id;
            string time_per = null;
            int path_cost = 0;
            string node_seq = null;
            string time_seq = null;
            for (int i = 0; i < path.Length; i++)
            {
                int[] sst = node_num2sst(path[i]);
                int node_row_num = (sst[0] - 1) * r.time_len + sst[2];
                int zone_id = get_zone_id(sst[0], sst[2]);
                if (i == 0)
                {
                    now_agent[1] = zone_id;
                    now_agent[3] = sst[0] * 1000000 + time_add2(r.start_time, sst[2] - 1);
                    time_per += time_int2string(time_add2(r.start_time, sst[2] - 1)) + "_";
                    path_cost = sst[2];
                }
                if (i == path.Length - 1)
                {
                    now_agent[2] = zone_id;
                    now_agent[4] = sst[0] * 1000000 + time_add2(r.start_time, sst[2]);
                    time_per += time_int2string(time_add2(r.start_time, sst[2] - 1));
                    path_cost = sst[2] - path_cost;
                }
                time_seq += time_int2string(time_add2(r.start_time, sst[2] - 1)) + ";";
                node_seq += (sst[0] * 1000000 + time_add2(r.start_time, sst[2] - 1)).ToString() + ";";
                node.Rows[node_row_num - 1][3] = zone_id;
                node.Rows[node_row_num - 1][4] = 1;
            }
            now_agent[5] = r.trian_type[r.total_train_num];
            now_agent[6] = time_per;
            now_agent[7] = 1;
            for (int i = 8; i <= 10; i++)
            {
                now_agent[i] = path_cost;
            }
            now_agent[11] = node_seq;
            now_agent[12] = time_seq;
            agent.Rows.Add(now_agent);
            for (int i = 0; i < path.Length - 1; i++)
            {
                int[] now_node = node_num2sst(path[i]);
                int[] next_node = node_num2sst(path[i + 1]);
                DataRow dr = road_link.NewRow();
                dr[1] = road_link_id++;
                int from_node_id = (now_node[0] * 1000000) + time_add2(r.start_time, now_node[2] - 1);
                int to_node_id = (next_node[0] * 1000000) + time_add2(r.start_time, next_node[2] - 1);
                int cost = next_node[2] - now_node[2];
                dr[2] = from_node_id;
                dr[3] = to_node_id;
                dr[5] = 1;
                dr[6] = cost;
                for (int ii = 7; ii <= 10; ii++)
                {
                    dr[ii] = 1;
                }
                dr[11] = cost;
                road_link.Rows.Add(dr);
            }

        }
        public DataTable init_node(DataTable node)
        {
            for (int i = 1; i <= r.station_num; i++)
            {
                for (int t = 1; t <= r.time_len; t++)
                {
                    DataRow dr = node.NewRow();
                    dr[1] = i;
                    dr[2] = i * 1000000 + time_add2(r.start_time, t - 1);
                    dr[3] = 0;
                    dr[4] = 0;
                    dr[6] = t * 100;
                    dr[7] = i * 1000;
                    node.Rows.Add(dr);
                }
            }
            return node;
        }
        //工具函数
        private int get_zone_id(int now_station, int now_time)
        {
            if (now_station != 1 && now_station != r.station_num)
            {
                return 0;
            }
            now_time = time_add2(r.start_time, now_time);
            for (int i = 0; i < r.zone.Rows.Count; i++)
            {
                int check_station = int.Parse((string)r.zone.Rows[i][3]);
                if (now_time >= int.Parse((string)r.zone.Rows[i][1]) && now_time <= int.Parse((string)r.zone.Rows[i][2]) && now_station == check_station)
                {
                    return i + 1;
                }
            }
            return 0;
        }
        private static int[] 合并数组(int[] a1, int[] a2)
        {
            int l_a1 = a1.Length;
            int l_a2 = a2.Length;
            int[] a3 = new int[l_a1 + l_a2];
            for (int i = 1; i <= l_a1 + l_a2; i++)
            {
                if (i <= l_a1)
                {
                    a3[i - 1] = a1[i - 1];
                }
                else
                {
                    a3[i - 1] = a2[i - l_a1 - 1];
                }
            }
            return a3;
        }
        private static int[] del(int[] a)
        {
            int[] a0 = new int[a.Length - 2];
            for (int i = 1; i < a.Length - 1; i++)
            {
                a0[i - 1] = a[i];
            }
            return a0;
        }
        private int sst2node_num(int station, int state, int time)//"time" start from 0,"state":1-arrive,2-depart,3-pass
        {
            if (station == 1)
            {
                return time + 2;
            }
            else if (station == r.station_num)
            {
                return (station - 2) * 3 * r.time_len + r.time_len + time + 2;
            }
            else
            {
                return (station - 2) * 3 * r.time_len + r.time_len + (state - 1) * r.time_len + time + 2;
            }
        }
        private int[] node_num2sst(int node_num)
        {
            int station, state, time;
            int num = (node_num - 2) / r.time_len;
            time = (node_num - 2) % r.time_len;
            if (time == 0)
            {
                time = r.time_len;
            }
            if (num >= 1)
            {
                station = (num - 1) / 3 + 2;
                state = num - 1 - (station - 2) * 3 + 1;
            }
            else
            {
                station = 1;
                if (station_range[0] == 1)
                {
                    state = 2;
                }
                else
                {
                    state = 1;
                }
            }
            if (station == r.station_num)
            {
                if (station_range[0] == 1)
                {
                    state = 1;
                }
                else
                {
                    state = 2;
                }
            }
            return new int[3] { station, state, time };
        }
        private void link_station(double[,] network, int now_station, int next_station, int running_time0, bool now_stop, bool next_stop, double[,] miu)
        {
            List<int> now_states = new List<int>(), next_states = new List<int>();
            if (now_stop == false && now_station != station_range[0])
            {
                now_states.Add(2); now_states.Add(3);
            }
            else
            {
                now_states.Add(2);
            }
            if (next_stop == false && next_station != station_range.Last())
            {
                next_states.Add(1); next_states.Add(3);
            }
            else
            {
                next_states.Add(1);
            }
            for (int now_time = 1; now_time <= r.time_len; now_time++)
            {
                foreach (var now_state in now_states)
                {
                    int running_time = running_time0;
                    int now_node = sst2node_num(now_station, now_state, now_time);
                    if (now_state == 2)
                        running_time += r.add_start;
                    foreach (var next_state in next_states)
                    {
                        if (next_state == 1)
                            running_time += r.add_stop;
                        for (int next_time = now_time + running_time; next_time <= r.time_len; next_time++)
                        {
                            int next_node = sst2node_num(next_station, next_state, next_time);
                            network[now_node - 1, next_node - 1] = running_time;
                            for (int tt = next_time - tf; tt < next_time + tp; tt++)
                            {
                                if (tt < 1 || tt > r.time_len)
                                    continue;
                                int ii = (next_station - 2) * 2 + 2;
                                network[now_node - 1, next_node - 1] -= miu[ii - 1, tt - 1];
                            }
                        }
                    }
                }
            }
        }
        public List<int[]> path_node2path_sst(int[] path)
        {
            List<int[]> path_sst = new List<int[]>();
            for (int i = 0; i < path.Length; i++)
            {
                path_sst.Add(node_num2sst(path[i]));
            }
            return path_sst;
        }
        public void return_time(int[] path_, int station, out double arr_time, out double dep_time)
        {
            List<int[]> path = path_node2path_sst(path_);
            arr_time = 0; dep_time = 0;
            for (int i = 0; i < path.Count; i++)
            {
                if (path[i][0] == station)
                {
                    if (path[i][1] == 1 || path[i][1] == 2)
                    {
                        if (station != station_range[0] && station != station_range.Last())
                        {
                            arr_time = path[i][2];
                            dep_time = path[i + 1][2];
                            return;
                        }
                        else if (station == station_range[0])
                        {
                            arr_time = 0;
                            dep_time = path[i][2];
                            return;
                        }
                        else
                        {
                            dep_time = 0;
                            arr_time = path[i][2];
                            return;
                        }
                    }
                    else
                    {
                        arr_time = path[i][2];
                        dep_time = path[i][2];
                        return;
                    }
                }
            }
        }
        private int time_add2(int t1, int t2)
        {
            int h1 = t1 / 100;
            int m1 = t1 % 100;
            if (m1 + t2 < 60)
            {
                return (h1) * 100 + (m1 + t2);
            }
            else
            {
                int add_h = (m1 + t2) / 60;
                int add_m = (m1 + t2) % 60;
                return (h1 + add_h) * 100 + add_m;
            }
        }
        private string time_int2string(int t)
        {
            if (t < 1000)
            {
                return "0" + t.ToString();
            }
            else
            {
                return t.ToString();
            }
        }
    }
}
