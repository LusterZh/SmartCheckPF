using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace samrtCheckPF.json
{

    public class GetCalibrationRes
    {
        public int[] roi_t { get; set; }
        public int[] roi_b { get; set; }
        public int[] roi_l { get; set; }
        public int[] roi_r { get; set; }
        public string client_url { get; set; }
        public bool use_deep_learning { get; set; }
        public bool seg_use_gpu { get; set; }
        public string server_url { get; set; }
        public string server_max_mission { get; set; }
        public DLConfig DL_config { get; set; }
    }

    public class DLConfig
    {
        public bool use_gpu { get; set; }
        public int[] input_size { get; set; }
        public string net_name { get; set; }
        public int max_epochs { get; set; }
        public double lr { get; set; }
        public int batch_size { get; set; }
    }




}
