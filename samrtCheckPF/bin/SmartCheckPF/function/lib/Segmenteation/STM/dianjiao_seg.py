from .dianjiao_models import STM

import torch
import numpy as np

class STM_GluePathInspection(object):
    def __init__(self, device='cuda'):
        self.net = None 
        self.device = device
        self.mem_key = None 
        self.mem_val = None 

    def load_model(self, model_path):
        keydim = 32
        valdim = 128
        self.net = STM(keydim, valdim)
        self.net.eval()
        # Load checkpoint.
        checkpoint = torch.load(model_path, map_location=self.device)
        # state = checkpoint['state_dict']
        state = checkpoint['state']
        self.mem_key = checkpoint['mem_key']
        self.mem_val = checkpoint['mem_val']
        self.net.load_param(state)
        

        if self.device == "cuda" or self.device == "gpu":
            self.net.to(self.device)
        

        # test_img = torch.empty(360, 2450, 3)
        # self.predict(test_img)
        # if self.device == "cuda" or self.device=="gpu":
        #     self.net.cuda()
        #     test_img = test_img.cuda()

        # self.net.segment(frame=test_img, keys=self.mem_key, values=self.mem_val, num_objects=1, max_obj=1)


    def predict(self, img):
        mean = np.array([0.485, 0.456, 0.406]).reshape([1, 1, 3]).astype(np.float32)
        std = np.array([0.229, 0.224, 0.225]).reshape([1, 1, 3]).astype(np.float32)
        img = (np.array(img).astype(dtype=np.float32, copy=True) / 255.0 - mean) / std
        img = img[np.newaxis, :]
        img = torch.from_numpy(img.copy())
        img = img.permute(0, 3, 1, 2).contiguous()
        max_obj = 1
        num_object = 1
        if self.device == "cuda" or self.device == "gpu":
            img = img.to("cuda")
        logits, ps = self.net.segment(frame=img, keys=self.mem_key, values=self.mem_val, 
            num_objects=num_object, max_obj=max_obj)

        out = torch.softmax(logits, dim=1)
        pred = out.detach().cpu().numpy()

        for t in range(pred.shape[0]):
            m = pred[t, :, :, :]
            mask = m.transpose((1, 2, 0))
            mask = mask.argmax(axis=2).astype(np.uint8)*255

        return mask


# def convert_mask(mask, max_obj):

#     # convert mask to one hot encoded
#     oh = []
#     for k in range(max_obj+1):
#         oh.append(mask==k)

#     oh = np.stack(oh, axis=2)

#     return oh

# def Initial_model(model_path):
#     device = "cuda" if use_gpu else "cpu"

#     keydim = 32
#     valdim = 128
#     net = STM(keydim, valdim)
    
#     # Load checkpoint.
#     checkpoint = torch.load(model_path, map_location=device)
#     # state = checkpoint['state_dict']
#     state = checkpoint['state']
#     mem_key = checkpoint['mem_key']
#     mem_val = checkpoint['mem_val']
#     net.load_param(state)
#     net.eval()

#     return net


# def Initial_model(model_path):
#     global mem_key,mem_val

#     device = "cuda" if use_gpu else "cpu"
#     max_obj = 1
#     num_object = 1
#     test_transformer = DianJiaoTestTransform()

#     golden_samples_name = [name.split(".")[0] for name in filter(lambda x: ".jpg" in x, os.listdir(golden_sample_dir))]

#     golden_samples_img = [np.array(Image.open(os.path.join(golden_sample_dir, name+'.jpg'))) for name in golden_samples_name]
#     golden_samples_mask = [np.array(Image.open(os.path.join(golden_sample_dir, name+'.png')).convert('L')) // 255 for name in golden_samples_name]
#     golden_samples_mask = [convert_mask(msk, max_obj) for msk in golden_samples_mask]
#     golden_samples_img, golden_samples_mask = test_transformer(golden_samples_img, golden_samples_mask)

#     keydim = 32
#     valdim = 128
#     net = STM(keydim, valdim)
#     net.eval()

#     test_img = torch.empty(1, 3, 360, 2450)
#     if use_gpu:
#         net.to(device)
#         golden_samples_img = golden_samples_img.to(device)
#         golden_samples_mask = golden_samples_mask.to(device)
#         test_img = test_img.to(device)
#     # Load checkpoint.
#     checkpoint = torch.load(model_path, map_location=device)
#     # state = checkpoint['state_dict']
#     state = checkpoint['state']
#     mem_key = checkpoint['mem_key']
#     mem_val = checkpoint['mem_val']
#     net.load_param(state)

#     # T, C, H, W = golden_samples_img.size()
#     # num_object = 1
#     # batch_keys = []
#     # batch_vals = []

#     # with torch.no_grad():
#     #     for t in range(1, T+1):
#     #         # memorize
#     #         print(f"caculate mem: {t}")
#     #         key, val, _ = net.memorize(frame=golden_samples_img[t-1:t], masks=golden_samples_mask[t-1:t], 
#     #             num_objects=num_object)

#     #         batch_keys.append(key)
#     #         batch_vals.append(val)

#     #     mem_key = torch.cat(batch_keys, dim=1)
#     #     mem_val = torch.cat(batch_vals, dim=1)

#     #     logits, ps = net.segment(frame=test_img, keys=mem_key, values=mem_val, 
#     #     num_objects=num_object, max_obj=max_obj)

#     # checkpoint = {}
#     # checkpoint['state'] = state
#     # checkpoint['mem_key'] = mem_key
#     # checkpoint['mem_val'] = mem_val

#     # torch.save(checkpoint, "function/STM/model_v2.pth")

#     return net


# def forward_by_img(net, img):
#     mean = np.array([0.485, 0.456, 0.406]).reshape([1, 1, 3]).astype(np.float32)
#     std = np.array([0.229, 0.224, 0.225]).reshape([1, 1, 3]).astype(np.float32)
#     img = (np.array(img).astype(dtype=np.float32, copy=True) / 255.0 - mean) / std
#     img = img[np.newaxis, :]
#     img = torch.from_numpy(img.copy())
#     img = img.permute(0, 3, 1, 2).contiguous()
#     max_obj = 1
#     num_object = 1
#     if use_gpu:
#         img = img.to("cuda")
#     logits, ps = net.segment(frame=img, keys=mem_key, values=mem_val, 
#         num_objects=num_object, max_obj=max_obj)

#     out = torch.softmax(logits, dim=1)
#     pred = out.detach().cpu().numpy()

#     for t in range(pred.shape[0]):
#         m = pred[t, :, :, :]
#         mask = m.transpose((1, 2, 0))
#         mask = mask.argmax(axis=2).astype(np.uint8)*255

#     return mask

def merge_img(image, mask):
    R = np.zeros_like(mask)
    B = np.zeros_like(mask)
    merged_mask = cv.merge([B, mask, R])
    final_img = image*0.9 + merged_mask*0.1
    return final_img

def predict(net, img):
    img = img[...,::-1]

    img_tl, img_tr, img_bl, img_br, img_l, img_r = split(img)
    img_l_rotate = np.rot90(img_l, -1)
    img_r_rotate = np.rot90(img_r)



    out_tl = forward_by_img(net, img_tl)
    out_tr = forward_by_img(net, img_tr)
    out_bl = forward_by_img(net, img_bl)
    out_br = forward_by_img(net, img_br)
    out_l  = forward_by_img(net, img_l_rotate)
    out_r  = forward_by_img(net, img_r_rotate)

    out_l_rotate = np.rot90(out_l)
    out_r_rotate = np.rot90(out_r, -1)
    out_mask = merge([out_tl, out_tr, out_bl, out_br, out_l_rotate, out_r_rotate])
    out_img = merge_img(img[...,::-1], out_mask)

    return out_img, out_mask



