from __future__ import absolute_import
from __future__ import division
from __future__ import print_function

import time
import cv2 as cv
from .utils.utils import *
from .dataset.dataLoader import *
from .models.models import *
current_path = os.path.dirname(__file__)
PROJECT = "defult"


class yolov3(object):
    def __init__(self, num_class, conf_threshold, device='cpu', cfg=None):
        param = cfg.Detector["YOLOv3"]
        self.DEVICE = param["DEVICE"]
        self.NUMBER_CLASS = param["NUMBER_CLASS"]
        
        if conf_threshold == None:
            self.CONF_THRESHOLD = param["CONF_THRESHOLD"]       # config 
        else:
            self.CONF_THRESHOLD = conf_threshold
            
        self.NMS_THRESHOLD = param["NMS_THRESHOLD"]
        self.YOLO_INPUT_SIZE = param["YOLO_INPUT_SIZE"]
        self.ORIGINAL_HEIGHT = param["ORIGINAL_HEIGHT"]
        self.ORIGINAL_WIDTH = param["ORIGINAL_WIDTH"]
        self.ORIGINAL_SHAPE = (self.ORIGINAL_HEIGHT, self.ORIGINAL_WIDTH)

        self.BACK_BONE = param["BACK_BONE"]
        if self.BACK_BONE == "Darknet":
            self.config_path = os.path.join(current_path, "models/yolov3_cl2_wh_320_ti20201109.cfg")
        elif self.BACK_BONE == "tiny":
            self.config_path = os.path.join(current_path, "models/tiny_YOLOv3.cfg")
        else:
            self.config_path = None

        self.model = Darknet(config_path=self.config_path, img_size=self.YOLO_INPUT_SIZE).to(device)

    def load_model(self, state_dict):
        self.model.load_state_dict(state_dict)
        if self.DEVICE == "gpu" or self.DEVICE == "cuda":
            self.model = self.model.cuda()

        self.model.eval()

    def predict(self, cv_img):
        start_time = time.time()
        result = []  # result: [{"bbox":[x,y,h,w], "score":score, "cls_id":cls_id}, ...]
        if cv_img is not None:
            output_tensor = self.net_forward(cv_img)
            net_forward_time = time.time()
            preprocess_and_forward_time = net_forward_time - start_time

            result = self.merge_output(output_tensor)

            merged_time = time.time()
            merge_time = merged_time - net_forward_time
            total_time = merged_time - start_time
            cost_time_or_error_info = f"{self.BACK_BONE}_YOLOv3 predict done, preprocess_and_forward_time: {preprocess_and_forward_time:.8f}sec, merge_output_time: {merge_time:.8f}sec,  total time: {total_time:.8f}sec"
        else:
            cost_time_or_error_info = f"ERROR: read image fail. please check if the path is correct."

        return result, cost_time_or_error_info

    def net_forward(self, image):
        Wo, Ho = self.ORIGINAL_SHAPE
        image = self.chack_size(image, self.ORIGINAL_SHAPE)

        W, H = image.shape[:2]
        cv_img = image[int(W / 2 - Wo / 2):int(W / 2 + Wo / 2), int(H / 2 - Ho / 2):int(H / 2 + Ho / 2), :]

        pil_img = Image.fromarray(cv.cvtColor(cv_img, cv.COLOR_BGR2RGB))
        input_imgs = transforms.ToTensor()(pil_img)
        input_imgs, _ = pad_to_square(input_imgs, 0)
        input_imgs = resize(input_imgs, self.YOLO_INPUT_SIZE)
        Tensor = torch.cuda.FloatTensor if (self.DEVICE == 'cuda' or self.DEVICE == 'gpu') else torch.FloatTensor
        input_imgs = Variable(input_imgs.type(Tensor))
        input_imgs = torch.unsqueeze(input_imgs, dim=0)

        with torch.no_grad():
            img_detections = self.model(input_imgs)
            img_detections = non_max_suppression(img_detections, self.CONF_THRESHOLD, self.NMS_THRESHOLD)

        return img_detections

    def merge_output(self, img_detections):
        result = []  # result: [{"bbox":[x,y,h,w], "score":score, "cls_id":cls_id}, ...]
        for detections in img_detections:
            if detections is None:
                continue
            # Rescale boxes to original image
            detections = rescale_boxes(detections, self.YOLO_INPUT_SIZE, original_shape=self.ORIGINAL_SHAPE)

            for x1, y1, x2, y2, conf, cls_conf_tensor, cls_pred in detections:
                x1 = x1.numpy()
                y1 = y1.numpy()
                x2 = x2.numpy()
                y2 = y2.numpy()
                bbox = [x1, y1, x2, y2]
                score = cls_conf_tensor.numpy()
                cls_id = int(cls_pred.numpy())
                result.append({"bbox": bbox, "score": score, "cls_id": cls_id})

        return result

    def chack_size(self, input_img, except_shzpe=(160, 448)):
        if input_img is None:
            return None

        W, H = input_img.shape[:2]

        if W % 2 == 1:
            W -= 1
        if H % 2 == 1:
            H -= 1
        input_img = input_img[0:W, 0:H, :]

        Wo, Ho = except_shzpe
        temp_img = np.zeros((Wo, Ho, 3))
        if W > 160:
            input_img = input_img[int(W / 2 - Wo / 2):int(W / 2 + Wo / 2), :, :]
        if H > 448:
            input_img = input_img[:, int(H / 2 - Ho / 2):int(H / 2 + Ho / 2), :]

        Wnew, Hnew = input_img.shape[:2]

        temp_img[int(Wo / 2 - Wnew / 2):int(Wo / 2 + Wnew / 2), int(Ho / 2 - Hnew / 2):int(Ho / 2 + Hnew / 2),
        :] = input_img

        return temp_img.astype(np.uint8)

    def non_max_suppression(self, prediction, conf_thres=0.5, nms_thres=0.4):
        """
        Removes detections with lower object confidence score than 'conf_thres' and performs
        Non-Maximum Suppression to further filter detections.
        Returns detections with shape:
            (x1, y1, x2, y2, object_conf, class_score, class_pred)
        """

        # From (center x, center y, width, height) to (x1, y1, x2, y2)
        prediction[..., :4] = xywh2xyxy(prediction[..., :4])
        output = [None for _ in range(len(prediction))]
        for image_i, image_pred in enumerate(prediction):
            # Filter out confidence scores below threshold
            image_pred = image_pred[image_pred[:, 4] >= conf_thres]
            # If none are remaining => process next image
            if not image_pred.size(0):
                continue
            # Object confidence times class confidence
            score = image_pred[:, 4] * image_pred[:, 5:].max(1)[0]
            # Sort by it
            image_pred = image_pred[(-score).argsort()]
            class_confs, class_preds = image_pred[:, 5:].max(1, keepdim=True)
            detections = torch.cat((image_pred[:, :5], class_confs.float(), class_preds.float()), 1)
            # Perform non-maximum suppression
            keep_boxes = []
            while detections.size(0):
                large_overlap = bbox_iou(detections[0, :4].unsqueeze(0), detections[:, :4]) > nms_thres
                label_match = detections[0, -1] == detections[:, -1]
                # Indices of boxes with lower confidence scores, large IOUs and matching labels
                invalid = large_overlap & label_match
                weights = detections[invalid, 4:5]
                # Merge overlapping bboxes by order of confidence
                detections[0, :4] = (weights * detections[invalid, :4]).sum(0) / weights.sum()
                keep_boxes += [detections[0]]
                detections = detections[~invalid]
            if keep_boxes:
                output[image_i] = torch.stack(keep_boxes)

        return output

    def train(self):
        from .YOLOv3_train import YOLOv3Trainer
        trainer = YOLOv3Trainer()
        trainer.train()



