import datetime
import time
# from terminaltables import AsciiTable
import torch
from .models.models import *
from .utils.utils import *
from ...config import system_config
from ...Logger import system_log
from .dataset.dataLoader import *
import tqdm
PROJECT = "defult"


class YOLOv3Trainer(object):
    def __init__(self):
        param = system_config.Detector["YOLOv3"][PROJECT]["train"]
        # ***********
        # self.num_class = num_class
        # self.detect_type = detect_type
        # self.train_folder = train_folder
        # self.val_folder = val_folder
        # self.model_root = model_root
        # self.version = version
        self.ckpt_version = ''

        # ***********
        self.epochs = param["epochs"]
        self.batch_size = param["batch_size"]
        self.gradient_accumulations = param["gradient_accumulations"]
        self.model_def = param["model_def"]
        self.data_config = param["data_config"]
        self.pretrained_weights = param["pretrained_weights"]
        self.n_cpu = param["n_cpu"]
        self.YOLO_INPUT_SIZE = param["YOLO_INPUT_SIZE"]
        self.checkpoint_interval = param["checkpoint_interval"]
        self.evaluation_interval = param["evaluation_interval"]
        self.compute_map = param["compute_map"]
        self.multiscale_training = param["multiscale_training"]
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")


    def train(self):
        # os.makedirs("output", exist_ok=True)
        # os.makedirs("checkpoints", exist_ok=True)

        # Get data configuration
        data_config = parse_data_config(self.data_config)
        train_path = data_config["train"]
        valid_path = data_config["valid"]
        class_names = load_classes(data_config["names"])

        # Initiate model
        model = Darknet(self.model_def).to(self.device)
        model.apply(weights_init_normal)

        # If specified we start from checkpoint
        if self.pretrained_weights and os.path.isfile(self.pretrained_weights):
            if self.pretrained_weights.endswith(".pth"):
                model.load_state_dict(torch.load(self.pretrained_weights))
            else:
                model.load_darknet_weights(self.pretrained_weights)

        # Get dataloader
        dataset = ListDataset(train_path, img_size=self.YOLO_INPUT_SIZE, augment=True,
                              multiscale=self.multiscale_training)
        dataloader = torch.utils.data.DataLoader(
            dataset,
            batch_size=self.batch_size,
            shuffle=True,
            num_workers=self.n_cpu,
            pin_memory=True,
            collate_fn=dataset.collate_fn,
        )

        optimizer = torch.optim.Adam(model.parameters())

        metrics = [
            "grid_size",
            "loss",
            "x",
            "y",
            "w",
            "h",
            "conf",
            "cls",
            "cls_acc",
            "recall50",
            "recall75",
            "precision",
            "conf_obj",
            "conf_noobj",
        ]

        for epoch in range(self.epochs):
            model.train()
            start_time = time.time()
            # Total_Loss = 10000
            for batch_i, (_, imgs, targets) in enumerate(dataloader):
                batches_done = len(dataloader) * epoch + batch_i

                imgs = Variable(imgs.to(self.device))
                targets = Variable(targets.to(self.device), requires_grad=False)

                loss, outputs = model(imgs, targets)
                loss.backward()

                if batches_done % self.gradient_accumulations:
                    # Accumulates gradient before each step
                    optimizer.step()
                    optimizer.zero_grad()

                # ----------------
                #   Log progress
                # ----------------
                # invalid syntax
                log_str = "\n---- [Epoch %d/%d, Batch %d/%d] ----\n" % (epoch, self.epochs, batch_i, len(dataloader))

                system_log.WriteLine(f"")

                metric_table = [["Metrics", *[f"YOLO Layer {i}" for i in range(len(model.yolo_layers))]]]

                # Log metrics at each YOLO layer
                for i, metric in enumerate(metrics):
                    formats = {m: "%.6f" for m in metrics}
                    formats["grid_size"] = "%2d"
                    formats["cls_acc"] = "%.2f%%"
                    row_metrics = [formats[metric] % yolo.metrics.get(metric, 0) for yolo in model.yolo_layers]
                    metric_table += [[metric, *row_metrics]]

                    # Tensorboard logging
                    tensorboard_log = []
                    for j, yolo in enumerate(model.yolo_layers):
                        for name, metric in yolo.metrics.items():
                            if name != "grid_size":
                                tensorboard_log += [(f"{name}_{j + 1}", metric)]
                    tensorboard_log += [("loss", loss.item())]
                    # logger.list_of_scalars_summary(tensorboard_log, batches_done)

                # log_str += AsciiTable(metric_table).table
                log_str += f"\nTotal loss {loss.item()}"
                # Total_Loss = f"Tloss{loss.item()}"
                # Determine approximate time left for epoch
                epoch_batches_left = len(dataloader) - (batch_i + 1)
                time_left = datetime.timedelta(seconds=epoch_batches_left * (time.time() - start_time) / (batch_i + 1))
                log_str += f"\n---- ETA {time_left}"

                print(log_str)

                model.seen += imgs.size(0)

            if epoch % self.evaluation_interval == 0:
                print("\n---- Evaluating Model ----")
                # Evaluate the model on the validation set
                precision, recall, AP, f1, ap_class = self.evaluate(
                    model,
                    path=valid_path,
                    iou_thres=0.5,
                    conf_thres=0.5,
                    nms_thres=0.5,
                    img_size=self.YOLO_INPUT_SIZE,
                    batch_size=8,
                )
                val_MAP = AP.mean()
                evaluation_metrics = [
                    ("val_precision", precision.mean()),
                    ("val_recall", recall.mean()),
                    ("val_mAP", AP.mean()),
                    ("val_f1", f1.mean()),
                ]
                # logger.list_of_scalars_summary(evaluation_metrics, epoch)

                # Print class APs and mAP
                ap_table = [["Index", "Class name", "AP"]]
                for i, c in enumerate(ap_class):
                    ap_table += [[c, class_names[c], "%.5f" % AP[i]]]
                # print(AsciiTable(ap_table).table)
                print(f"---- mAP {AP.mean()}")

            if epoch % self.checkpoint_interval == 0 and val_MAP > 0.7:
                def save_model(path, epoch, model, model_name, num_class, detection_type, version=None):
                    if isinstance(model, torch.nn.DataParallel):
                        state_dict = model.module.state_dict()
                    else:
                        state_dict = model.state_dict()
                    data = {
                        "detection_type": detection_type,
                        "model_name": model_name,
                        "version": version,
                        "epoch": epoch,
                        "state_dict": state_dict,
                        "num_class": num_class
                    }

                    torch.save(data, path)
                    print(f"save model to {path}")
                    # system_log.WriteLine(f"save model to {path}")

                save_model(f"checkpoints/leshizhen_yolov3_ckpt_{epoch}_map{val_MAP}.pth", epoch, model, 'yolov3', 3,
                           'yolo', version=self.ckpt_version)
                torch.save(model.state_dict(), f"checkpoints/yolov3_ckpt_{epoch}_map{val_MAP}.pth")
        pass

    def evaluate(self, model, path, iou_thres, conf_thres, nms_thres, img_size, batch_size):

        model.eval()

        # Get dataloader
        dataset = ListDataset(path, img_size=img_size, augment=False, multiscale=False)
        dataloader = torch.utils.data.DataLoader(
            dataset, batch_size=batch_size, shuffle=False, num_workers=1, collate_fn=dataset.collate_fn
        )

        Tensor = torch.cuda.FloatTensor if torch.cuda.is_available() else torch.FloatTensor
        # Tensor = torch.FloatTensor   # 使用cpu evaluate

        labels = []
        sample_metrics = []  # List of tuples (TP, confs, pred)
        for batch_i, (_, imgs, targets) in enumerate(tqdm.tqdm(dataloader, desc="Detecting objects")):
            # Extract labels
            labels += targets[:, 1].tolist()
            # Rescale target
            targets[:, 2:] = xywh2xyxy(targets[:, 2:])
            targets[:, 2:] *= img_size
            temp1 = targets.numpy()
            imgs = Variable(imgs.type(Tensor), requires_grad=False)

            with torch.no_grad():
                outputs = model(imgs)
                outputs = non_max_suppression(outputs, conf_thres=conf_thres, nms_thres=nms_thres)

            temp = get_batch_statistics(outputs, targets, iou_threshold=iou_thres)
            sample_metrics += get_batch_statistics(outputs, targets, iou_threshold=iou_thres)

        # Concatenate sample statistics
        true_positives, pred_scores, pred_labels = [np.concatenate(x, 0) for x in list(zip(*sample_metrics))]
        precision, recall, AP, f1, ap_class = ap_per_class(true_positives, pred_scores, pred_labels, labels)

        return precision, recall, AP, f1, ap_class

