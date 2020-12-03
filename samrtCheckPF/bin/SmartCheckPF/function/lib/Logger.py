import os
from datetime import datetime

class Logger:
    def __init__(self):
        self.f = None
        self.root = None
    def set_filepath(self, log_path):
        (file_root, file_name) = os.path.split(log_path)
        self.root = file_root

        if os.path.exists(log_path):
            os.remove(log_path)
        self.f = open(log_path,'w')

    def WriteLine(self, str):
        now = datetime.now()
        now_str =  '%02d-%02d %02d:%02d:%02d'%(now.month,now.day,now.hour,now.minute,now.second)
        if self.f is not None:
            self.f.write(now_str+"  "+str+'\n')
            self.f.flush()
        print(now_str+"  "+str)

    def get_root(self):
        return self.root

    def close(self):
        if self.f is not None:
            self.f.close()
            self.f = None
            self.root = None
            
system_log = Logger()
