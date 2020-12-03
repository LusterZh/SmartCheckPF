from .STM.dianjiao_seg import STM_GluePathInspection


class Segment(object):
    def __init__(self, net_name, device='cuda'):
        
        if net_name == "STM":
            self.model = STM_GluePathInspection(device)

    def load_model(self, model_path):
        self.model.load_model(model_path) 

    def predict(self, cv_img):
        mask = self.model.predict(cv_img)
        return mask 