from lib.Detection.DetectTest import DetectTest
from lib.config import client_config
import json

if __name__ == "__main__":
    # net_name = "MobileNet_CenterNet"
    net_name = "YOLOv3"
    num_class = 2
    golden_sample_path = ""
    model_path = "E:/PYCHARMPRO/YOLOV3/PyTorch-YOLOv3_surf_1109/checkpoints/leshizhen_yolov3_ckpt_13_map1.0.pth"
    threshold = 0.4
    device = "cpu"

    # client_config = None
    cfg_file = "lib/client.json"
    with open(cfg_file, "r") as f:
        client_config.update_config(json.load(f))


    test_folder = 'E:/DATASET/Surf/yolov3/test/test0'
    # root = 'E:/DATASET/Surf/yolov3/test'
    detector = DetectTest(net_name, num_class, golden_sample_path, model_path, threshold, device, client_config)
    detector.run(test_folder)
