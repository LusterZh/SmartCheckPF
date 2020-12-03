from __future__ import absolute_import
from __future__ import division
from __future__ import print_function

import torch
import torch.nn.functional as F
import numpy as np 

from .utils.utils import _transpose_and_gather_feat, _gather_feat, bbox_xyxy2xyhw
from .models.networks.MCNet import MCNet

import time


def _nms(heat, kernel=3):
    pad = (kernel - 1) // 2

    hmax = F.max_pool2d(
        heat, (kernel, kernel), stride=1, padding=pad)
    keep = (hmax == heat).float()
    return heat * keep

def _topk(scores, K=40):
    batch, cat, height, width = scores.size()
      
    topk_scores, topk_inds = torch.topk(scores.view(batch, cat, -1), K)

    topk_inds = topk_inds % (height * width)
    topk_ys   = (topk_inds / width).int().float()
    topk_xs   = (topk_inds % width).int().float()
      
    topk_score, topk_ind = torch.topk(topk_scores.view(batch, -1), K)
    topk_clses = (topk_ind / K).int()
    topk_inds = _gather_feat(
        topk_inds.view(batch, -1, 1), topk_ind).view(batch, K)
    topk_ys = _gather_feat(topk_ys.view(batch, -1, 1), topk_ind).view(batch, K)
    topk_xs = _gather_feat(topk_xs.view(batch, -1, 1), topk_ind).view(batch, K)

    return topk_score, topk_inds, topk_clses, topk_ys, topk_xs

class MobileNet_CenterNet(object):
    def __init__(self, num_class, threshold, device='cpu'):
        self.model = MCNet(num_class)
        self.device = device
        self.threshold = threshold

    def load_model(self, state_dict):
        self.model.load_state_dict(state_dict)
        if self.device == "gpu" or self.device == "cuda":
            self.model = self.model.cuda()

        self.model.eval()

    def decode(self, heat, wh, reg=None, K=100):
        batch, cat, height, width = heat.size()

        # heat = torch.sigmoid(heat)
        # perform nms on heatmaps
        nms_kernel = 7
        heat = _nms(heat, nms_kernel)

        # draw heat
        # draw_heat = heat.detach().numpy()[0]
        # draw_heat = draw_heat.transpose(1,2,0)
        # cv.imwrite("heat.jpg", draw_heat*255)
      
        scores, inds, clses, ys, xs = _topk(heat, K=K)
        if reg is not None:
            reg = _transpose_and_gather_feat(reg, inds)
            reg = reg.view(batch, K, 2)
            xs = xs.view(batch, K, 1) + reg[:, :, 0:1]
            ys = ys.view(batch, K, 1) + reg[:, :, 1:2]
        else:
            xs = xs.view(batch, K, 1) + 0.5
            ys = ys.view(batch, K, 1) + 0.5
        wh = _transpose_and_gather_feat(wh, inds)
        wh = wh.view(batch, K, 2)
        clses  = clses.view(batch, K, 1).float()
        scores = scores.view(batch, K, 1)
        bboxes = torch.cat([xs - wh[..., 0:1] / 2, 
                            ys - wh[..., 1:2] / 2,
                            xs + wh[..., 0:1] / 2, 
                            ys + wh[..., 1:2] / 2], dim=2)
        detections = torch.cat([bboxes, scores, clses], dim=2)
        
        return detections

    def merge_output(self, dets):
        # dets.shape: n,K,6. {x1,y1,x2,y2,score,cls_id}
        down_ratio = 4
        if self.device == "gpu" or self.device == "cuda":
            dets = dets.cpu()

        dets = dets.numpy()[0]      # shape: K,6
        obj_num, _ = dets.shape

        result = []     # result: [{"bbox":[x,y,h,w], "score":score, "cls_id":cls_id}, ...]

        for i in range(obj_num):
            if dets[i][4] > self.threshold:
                det = {}
                bbox = [dets[i][0]*down_ratio, dets[i][1]*down_ratio, dets[i][2]*down_ratio, dets[i][3]*down_ratio]
                bbox = bbox_xyxy2xyhw(bbox)
                score = dets[i][4]
                cls_id = dets[i][5]

                det["bbox"] = bbox
                det["score"] = score
                det["cls_id"] = cls_id

                result.append(det)

        return result

    def pre_process(self, image):
    
        image = (image.astype(np.float32) / 255.)

        image = image.astype(np.float32).transpose(2,0,1)
        image_tensor = torch.Tensor(image)
        image_tensor = torch.unsqueeze(image_tensor, 0)

        if self.device == "gpu" or self.device == "cuda":
            image_tensor = image_tensor.cuda()
        
        return image_tensor

    def process(self, tensor):
        with torch.no_grad():
            output_dict = self.model(tensor)     # output_dict: {"hm": hm, "reg": reg, "wh": wh}, hm.shape: n,cls_num,output_h,output_w,  reg.shape: n,2,output_h,output_w,  wh.shape: n,2,output_h,output_w

            hm = output_dict["hm"]
            reg = output_dict["reg"]
            wh = output_dict["wh"]

            detections = self.decode(hm, wh, reg=reg, K=64)     # detections.shape: n,K,6. {x1,y1,x2,y2,score,cls_id}


        return detections

    def predict(self, cv_img):
        pre_time, forward_time, merge_time, total_time = 0, 0, 0, 0
        start_time = time.time()

        result = []
        cost_time_or_error_info = None

        if cv_img is not None:
            input_tensor = self.pre_process(cv_img)
            preprocess_time = time.time()
            pre_time = preprocess_time - start_time

            output_tensor = self.process(input_tensor).detach()
            net_forward_time = time.time()
            forward_time = net_forward_time-preprocess_time

            result = self.merge_output(output_tensor)       # result: [{"bbox":[x,y,h,w], "score":score, "cls_id":cls_id}, ...]
            merged_time = time.time()
            merge_time = merged_time - net_forward_time
            total_time = merged_time - start_time

        else:
            cost_time_or_error_info = f"ERROR: read image fail. please check if the path is correct."

        cost_time_or_error_info = f"MobileNet_CenterNet preprocess_time: {pre_time:.8f}sec, net_forward_time: {forward_time:.8f}sec, merge_output_time: {merge_time:.8f}sec,  total time: {total_time:.8f}sec"

        return result, cost_time_or_error_info
    
