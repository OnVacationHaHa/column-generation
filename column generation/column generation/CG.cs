using MathWorks.MATLAB.NET.Arrays;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace column_generation
{
    class CG
    {
        read_file r;
        public void main()
        {
            train[] trains = new train[r.total_train_num];
            for (int i = 0; i < trains.Length; i++)
            {
                trains[i] = new train(r, i + 1);
            }
            init_solution(trains);
            bool any_neg = true;
            int iteration = 1;
            while (any_neg == true)
            {
                Console.WriteLine("*****************************************");
                Console.WriteLine("正在进行第" + iteration++.ToString() + "次迭代计算");
                if (iteration==2)
                {
                    Console.WriteLine("正在初始化引用");
                }
                double[] dual_var = dual();
                if (iteration==2)
                {
                    Console.WriteLine("初始化引用完毕");
                }
                int num = 0;
                for (int i = 0; i < resource_num; i++)
                {
                    for (int j = 0; j < r.time_len; j++)
                    {
                        miu[i, j] = dual_var[num++];
                    }
                }
                sigma = new double[r.total_train_num];
                for (int i = 0; i < r.total_train_num; i++)
                {
                    sigma[i] = dual_var[num++];
                }
                //solve reduced cost
                List<int[]> new_paths = new List<int[]>();
                for (int i = 0; i < r.total_train_num; i++)
                {
                    new_paths.Add(trains[i].get_path(miu));
                }
                any_neg = false;
                for (int i = 0; i < r.total_train_num; i++)
                {
                    int[,] col = trains[i].column(new_paths[i]);
                    int cost = trains[i].cost(new_paths[i]);
                    double reduced_cost = cost;
                    for (int j = 0; j < resource_num; j++)
                    {
                        for (int t = 0; t < r.time_len; t++)
                        {
                            reduced_cost -= miu[j, t] * col[j, t];
                        }
                    }
                    reduced_cost -= sigma[i];
                    if (reduced_cost < 0)
                    {
                        column_pool[i].Add(col);
                        cost_pool[i].Add(cost);
                        path_pool[i].Add(new_paths[i]);
                        any_neg = true;
                    }
                }
            }
            double[] opt_solution = simplex();
            int col_num = 0;
            List<List<double>> opt_solution_ = new List<List<double>>();
            for (int i = 0; i < cost_pool.Length; i++)
            {
                List<double> ld = new List<double>();
                opt_solution_.Add(ld);
                for (int j = 0; j < cost_pool[i].Count; j++)
                {
                    opt_solution_[i].Add(opt_solution[col_num++]);
                }
            }
            viable(opt_solution_, trains);

        }
        public CG(read_file r)
        {
            column_pool = new List<int[,]>[r.total_train_num];
            cost_pool = new List<int>[r.total_train_num];
            path_pool = new List<int[]>[r.total_train_num];
            for (int i = 0; i < column_pool.Length; i++)
            {
                column_pool[i] = new List<int[,]>();
                cost_pool[i] = new List<int>();
                path_pool[i] = new List<int[]>();
            }
            this.r = r;
            resource_num = (r.station_num - 2) * 2 + 2;
            miu = new double[resource_num, r.time_len];
            sigma = new double[r.total_train_num];
        }

        List<int[,]>[] column_pool;
        List<int>[] cost_pool;
        List<int[]>[] path_pool;
        int resource_num;
        double[,] miu;
        double[] sigma;

        private void init_solution(train[] trains)
        {
            for (int i = 0; i < trains.Length; i++)
            {
                int[] path = trains[i].get_path(miu);
                column_pool[i].Add(trains[i].column(path));
                cost_pool[i].Add(trains[i].cost(path));
                path_pool[i].Add(path);
            }
        }

        private double[] dual()
        {
            int rows = (resource_num) * r.time_len;
            int cols = 0;
            for (int i = 0; i < cost_pool.Length; i++)
            {
                cols += cost_pool[i].Count;
            }
            // A赋值
            MWArray A = (MWNumericArray)new double[rows, cols];
            double[,] AA = new double[rows, cols];
            int row = 0, col = 0;
            for (int i = 0; i < resource_num; i++)
            {
                for (int t = 0; t < r.time_len; t++)
                {
                    col = 0;
                    for (int f = 0; f < r.total_train_num; f++)
                    {
                        for (int j = 0; j < column_pool[f].Count; j++)
                        {
                            AA[row, col++] = column_pool[f][j][i, t];
                        }
                    }
                    row++;
                }
            }
            A = (MWNumericArray)AA;
            // B赋值
            MWArray B = (MWNumericArray)new double[rows];
            double[] B_ = new double[rows];
            for (int i = 0; i < rows; i++)
            {
                B_[i] = 1;
            }
            B = (MWNumericArray)B_;
            // lb ub 赋值
            MWArray lb = (MWNumericArray)new double[cols];
            MWArray ub = (MWNumericArray)new double[cols];
            double[] lb_ = new double[cols];
            double[] ub_ = new double[cols];
            for (int i = 0; i < cols; i++)
            {
                lb_[i] = 0; ub_[i] = 1;
            }
            lb = (MWNumericArray)lb_; ub = (MWNumericArray)ub_;
            // Aeq 赋值
            MWArray Aeq = (MWNumericArray)new double[r.total_train_num, cols];
            double[,] Aeq_ = new double[r.total_train_num, cols];
            col = 0;
            for (int i = 0; i < r.total_train_num; i++)
            {
                for (int j = 0; j < cost_pool[i].Count; j++)
                {
                    Aeq_[i, col] = 1;
                    col++;
                }
            }
            Aeq = (MWNumericArray)Aeq_;
            //Beq赋值
            MWArray Beq = (MWNumericArray)new double[r.total_train_num];
            double[] Beq_ = new double[r.total_train_num];
            for (int i = 0; i < r.total_train_num; i++)
            {
                Beq_[i] = 1;
            }
            Beq = (MWNumericArray)Beq_;
            //F赋值
            MWArray F = (MWNumericArray)new double[cols];
            double[] F_ = new double[cols];
            col = 0;
            for (int i = 0; i < cost_pool.Length; i++)
            {
                for (int j = 0; j < cost_pool[i].Count; j++)
                {
                    F_[col++] = cost_pool[i][j];
                }
            }
            F = (MWNumericArray)F_;
            //输入参数
            MWArray[] agrsIn = new MWArray[] { (MWNumericArray)F, (MWNumericArray)A, (MWNumericArray)B, (MWNumericArray)Aeq, (MWNumericArray)Beq, (MWNumericArray)lb, (MWNumericArray)ub };//输入参数
            //输出存放的数组
            MWArray[] agrsOut = new MWArray[2];
            dual.Class1 dual = new dual.Class1();
            dual.dual(2, ref agrsOut, agrsIn);
            double[] opt_solution = new double[rows + Aeq_.GetLength(0)];
            MWNumericArray opt_solution_ = (MWNumericArray)agrsOut[0];
            for (int i = 0; i < rows + r.total_train_num; i++)
            {
                opt_solution[i] = opt_solution_[i + 1, 1].ToScalarDouble();
            }
            return opt_solution;
        }

        private double[] simplex()
        {
            int rows = resource_num * r.time_len;
            int cols = 0;
            for (int i = 0; i < cost_pool.Length; i++)
            {
                cols += cost_pool[i].Count;
            }
            //A赋值
            MWArray A = (MWNumericArray)new double[rows, cols];
            double[,] AA = new double[rows, cols];
            int row = 0, col = 0;
            for (int i = 0; i < resource_num; i++)
            {
                for (int t = 0; t < r.time_len; t++)
                {
                    col = 0;
                    for (int f = 0; f < r.total_train_num; f++)
                    {
                        for (int j = 0; j < column_pool[f].Count; j++)
                        {
                            AA[row, col++] = column_pool[f][j][i, t];
                        }
                    }
                    row++;
                }
            }
            A = (MWNumericArray)AA;
            //B赋值
            MWArray B = (MWNumericArray)new double[rows];
            double[] B_ = new double[rows];
            for (int i = 0; i < rows; i++)
            {
                B_[i] = 1;
            }
            B = (MWNumericArray)B_;
            //Aeq赋值
            MWArray Aeq = (MWNumericArray)new double[r.total_train_num, cols];
            double[,] Aeq_ = new double[r.total_train_num, cols];
            col = 0;
            for (int i = 0; i < r.total_train_num; i++)
            {
                for (int j = 0; j < cost_pool[i].Count; j++)
                {
                    Aeq_[i, col] = 1;
                    col++;
                }
            }
            Aeq = (MWNumericArray)Aeq_;
            //Beq赋值
            MWArray Beq = (MWNumericArray)new double[r.total_train_num];
            double[] Beq_ = new double[r.total_train_num];
            for (int i = 0; i < r.total_train_num; i++)
            {
                Beq_[i] = 1;
            }
            Beq = (MWNumericArray)Beq_;
            //F赋值
            MWArray F = (MWNumericArray)new double[cols];
            double[] F_ = new double[cols];
            col = 0;
            for (int i = 0; i < cost_pool.Length; i++)
            {
                for (int j = 0; j < cost_pool[i].Count; j++)
                {
                    F_[col] = cost_pool[i][j];
                    col++;
                }
            }
            F = (MWNumericArray)F_;
            //输入参数
            MWArray[] agrsIn = new MWArray[] { (MWNumericArray)F, (MWNumericArray)A, (MWNumericArray)B, (MWNumericArray)Aeq, (MWNumericArray)Beq };
            //输出存放的数组
            MWArray[] agrsOut = new MWArray[2];
            simple.Class1 simplex = new simple.Class1();
            simplex.simple(2, ref agrsOut, agrsIn);
            double[] opt_solution = new double[cols];
            MWNumericArray opt_solution_ = (MWNumericArray)agrsOut[0];
            for (int i = 0; i < cols; i++)
            {
                opt_solution[i] = opt_solution_[i + 1, 1].ToScalarDouble();
            }
            return opt_solution;
        }

        private void viable(List<List<double>> opt_solution, train[] trains)
        {
            List<List<int[]>> solutions = new List<List<int[]>>();
            for (int i = 0; i < r.total_train_num; i++)
            {
                solutions.Add(new List<int[]>());
                for (int j = 0; j < r.station_num; j++)
                {
                    int now_station = trains[i].station_range[j];
                    double arr_time0 = 0, dep_time0 = 0;
                    for (int k = 0; k < opt_solution[i].Count; k++)
                    {
                        trains[i].return_time(path_pool[i][k], now_station, out double arr_time, out double dep_time);
                        arr_time0 += arr_time * opt_solution[i][k];
                        dep_time0 += dep_time * opt_solution[i][k];
                    }
                    arr_time0 = Math.Floor(arr_time0);
                    dep_time0 = Math.Floor(dep_time0);
                    solutions[i].Add(new int[2] { (int)arr_time0, (int)dep_time0 });
                }
            }
            for (int i = 0; i < r.station_num; i++)
            {
                sort_station(solutions, i + 1);
            }
            for (int i = 0; i < r.station_num - 1; i++)
            {
                sort_section(solutions, i + 1);
            }
            for (int i = 0; i < r.station_num; i++)
            {
                sort_station(solutions, i + 1);
            }
            DataTable node = define_node();
            DataTable road_link = define_road_link();
            DataTable agent = define_agent();
            trains[0].init_node(node);
            int agent_id = 1;
            for (int i = 0; i < trains.Length; i++)
            {
                trains[i].generate_nexta(solutions[i], i + 1, node, road_link, agent, ref agent_id);
            }
            string output_str = AppDomain.CurrentDomain.BaseDirectory + "output_file";
            if (!Directory.Exists(output_str))
                Directory.CreateDirectory(output_str);
            SaveCsv(node, output_str + "\\node");
            SaveCsv(road_link, output_str + "\\road_link");
            SaveCsv(agent, output_str + "\\agent");
            SaveCsv(r.agent_type, output_str + "\\agent_type");            
        }

        private void sort_station(List<List<int[]>> solutions, int station)
        {
            List<int[]> arr_solution = new List<int[]>();
            List<int[]> dep_solution = new List<int[]>();
            for (int i = 0; i < r.total_train_num; i++)
            {
                arr_solution.Add(new int[2] { i + 1, solutions[i][station - 1][0] });
                dep_solution.Add(new int[2] { i + 1, solutions[i][station - 1][1] });
            }
            if (station != 1)
            {
                arr_solution.Sort(delegate (int[] x, int[] y)
                {
                    return x[1].CompareTo(y[1]);
                });
                for (int i = 0; i < arr_solution.Count - 1; i++)
                {
                    int train_id1 = arr_solution[i][0];
                    int train_id2 = arr_solution[i + 1][0];
                    arr_interval(train_id1, train_id2, arr_solution[i][1], arr_solution[i + 1][1], out arr_solution[i + 1][1]);
                }
                for (int i = 0; i < arr_solution.Count; i++)
                {
                    int now_train = arr_solution[i][0];
                    solutions[now_train - 1][station - 1][0] = arr_solution[i][1];
                }
            }
            if (station != r.station_num)
            {
                dep_solution.Sort(delegate (int[] x, int[] y)
                {
                    return x[1].CompareTo(y[1]);
                });
                for (int i = 0; i < dep_solution.Count - 1; i++)
                {
                    int train_id1 = dep_solution[i][0];
                    int train_id2 = dep_solution[i + 1][0];
                    dep_interval(train_id1, train_id2, dep_solution[i][1], dep_solution[i + 1][1], out dep_solution[i + 1][1]);
                }
                if (station != 1)
                {
                    for (int i = 0; i < dep_solution.Count; i++)
                    {
                        int min_wait_time = int.Parse((string)r.min_waiting_time.Rows[i][station - 1]);
                        if (dep_solution[i][1] - arr_solution[i][1] < min_wait_time)
                        {
                            dep_solution[i][1] = arr_solution[i][1] + min_wait_time;
                        }
                    }
                }
                for (int i = 0; i < dep_solution.Count; i++)
                {
                    int now_train = dep_solution[i][0];
                    solutions[now_train - 1][station - 1][1] = dep_solution[i][1];
                }
            }
        }

        private void sort_section(List<List<int[]>> solutions, int start_station)
        {
            List<int[]> dep_solution = new List<int[]>();
            for (int i = 0; i < r.total_train_num; i++)
            {
                dep_solution.Add(new int[2] { i + 1, solutions[i][start_station - 1][1] });
            }
            dep_solution.Sort(delegate (int[] x, int[] y)
            {
                return x[1].CompareTo(y[1]);
            });
            for (int i = 0; i < r.total_train_num; i++)
            {
                int now_train = dep_solution[i][0];
                int running_time = int.Parse((string)r.running_time.Rows[now_train - 1][start_station]);
                if (solutions[now_train - 1][start_station - 1][0] != solutions[now_train - 1][start_station - 1][1])
                {
                    running_time += r.add_start;
                }
                if (solutions[now_train - 1][start_station][0] != solutions[now_train - 1][start_station][1])
                {
                    running_time += r.add_stop;
                }
                if (solutions[now_train - 1][start_station][0] - solutions[now_train - 1][start_station - 1][1] < running_time)
                {
                    int time_diff = running_time - (solutions[now_train - 1][start_station][0] - solutions[now_train - 1][start_station - 1][1]);
                    solutions[now_train - 1][start_station][0] += time_diff;
                    solutions[now_train - 1][start_station][1] += time_diff;

                }
            }
        }

        private void arr_interval(int train_id1, int train_id2, int arr1, int arr2, out int arr2_)
        {
            int tf1 = int.Parse((string)r.blocking_time.Rows[train_id1 - 1][1]);
            int tp1 = int.Parse((string)r.blocking_time.Rows[train_id1 - 1][2]);
            int tf2 = int.Parse((string)r.blocking_time.Rows[train_id2 - 1][1]);
            int tp2 = int.Parse((string)r.blocking_time.Rows[train_id2 - 1][2]);
            if (arr1 + tp1 + tf2 > arr2)
            {
                arr2_ = arr1 + tp1 + tf2;
            }
            else
            {
                arr2_ = arr2;
            }
        }

        private void dep_interval(int train_id1, int train_id2, int dep1, int dep2, out int dep2_)
        {
            int tf1 = int.Parse((string)r.blocking_time.Rows[train_id1 - 1][1]);
            int tp1 = int.Parse((string)r.blocking_time.Rows[train_id1 - 1][2]);
            int tf2 = int.Parse((string)r.blocking_time.Rows[train_id2 - 1][1]);
            int tp2 = int.Parse((string)r.blocking_time.Rows[train_id2 - 1][2]);
            if (dep1 + tp1 + tf2 > dep2)
            {
                dep2_ = dep1 + tp1 + tf2;
            }
            else
            {
                dep2_ = dep2;
            }
        }

        private void SaveCsv(DataTable dt, string filePath)
        {
            FileStream fs = null;
            StreamWriter sw = null;
            try
            {
                fs = new FileStream(filePath + dt.TableName + ".csv", FileMode.Create, FileAccess.Write);
                sw = new StreamWriter(fs, Encoding.Default);
                var data = string.Empty;
                //写出列名称
                for (var i = 0; i < dt.Columns.Count; i++)
                {
                    data += dt.Columns[i].ColumnName;
                    if (i < dt.Columns.Count - 1)
                    {
                        data += ",";
                    }
                }
                sw.WriteLine(data);
                //写出各行数据
                for (var i = 0; i < dt.Rows.Count; i++)
                {
                    data = string.Empty;
                    for (var j = 0; j < dt.Columns.Count; j++)
                    {
                        data += dt.Rows[i][j].ToString();
                        if (j < dt.Columns.Count - 1)
                        {
                            data += ",";
                        }
                    }
                    sw.WriteLine(data);
                }
            }
            catch (IOException ex)
            {
                throw new IOException(ex.Message, ex);
            }
            finally
            {
                if (sw != null) sw.Close();
                if (fs != null) fs.Close();
            }
        }

        private static DataTable define_agent()
        {
            DataTable agent = new DataTable();
            DataColumn agent_id = new DataColumn("agent_id", typeof(int));
            DataColumn o_zone_id = new DataColumn("o_zone_id", typeof(long));
            DataColumn d_zone_id = new DataColumn("d_zone_id", typeof(long));
            DataColumn o_node_id = new DataColumn("o_node_id", typeof(long));
            DataColumn d_node_id = new DataColumn("d_node_id", typeof(long));
            DataColumn agent_type = new DataColumn("agent_type", typeof(string));
            DataColumn time_period = new DataColumn("time_period", typeof(string));
            DataColumn volume = new DataColumn("volume", typeof(int));
            DataColumn cost = new DataColumn("cost", typeof(int));
            DataColumn travel_time = new DataColumn("travel_time", typeof(int));
            DataColumn distance = new DataColumn("distance", typeof(int));
            DataColumn node_sequence = new DataColumn("node_sequence", typeof(string));
            DataColumn time_sequence = new DataColumn("time_sequence", typeof(string));
            agent.Columns.AddRange(new DataColumn[13] { agent_id, o_zone_id, d_zone_id, o_node_id, d_node_id, agent_type, time_period, volume, cost, travel_time, distance, node_sequence, time_sequence });
            return agent;
        }
        private static DataTable define_road_link()
        {
            DataTable road_link = new DataTable();
            DataColumn name = new DataColumn("name", typeof(string));
            DataColumn road_link_id = new DataColumn("road_link_id", typeof(int));
            DataColumn from_node_id = new DataColumn("from_node_id", typeof(long));
            DataColumn to_node_id = new DataColumn("to_node_id", typeof(long));
            DataColumn facility_type = new DataColumn("facility_type", typeof(int));
            DataColumn dir_flag = new DataColumn("dir_flag", typeof(int));
            DataColumn length = new DataColumn("length", typeof(int));
            DataColumn lanes = new DataColumn("lanes", typeof(int));
            DataColumn capacity = new DataColumn("capacity", typeof(int));
            DataColumn free_speed = new DataColumn("free_speed", typeof(int));
            DataColumn link_type = new DataColumn("link_type", typeof(int));
            DataColumn cost = new DataColumn("cost", typeof(int));
            road_link.Columns.AddRange(new DataColumn[12] { name, road_link_id, from_node_id, to_node_id, facility_type, dir_flag, length, lanes, capacity, free_speed, link_type, cost });
            return road_link;
        }
        private static DataTable define_node()
        {
            DataTable node = new DataTable();
            DataColumn name = new DataColumn("name", typeof(string));
            DataColumn phy_node_id = new DataColumn("physical_node_id", typeof(int));
            DataColumn node_id = new DataColumn("node_id", typeof(long));
            DataColumn zone_id = new DataColumn("zone_id", typeof(long));
            DataColumn node_type = new DataColumn("node_type", typeof(int));
            DataColumn control_type = new DataColumn("control_type", typeof(int));
            DataColumn x_coord = new DataColumn("x_coord", typeof(int));
            DataColumn y_coord = new DataColumn("y_coord", typeof(int));
            node.Columns.AddRange(new DataColumn[8] { name, phy_node_id, node_id, zone_id, node_type, control_type, x_coord, y_coord });
            return node;
        }
    }
}
