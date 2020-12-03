import numpy as np 
import os 
import json 

current_path = os.path.dirname(__file__)


class ClientConfig(object):
    def __init__(self):
        self._configs = {}

        # System
        self._configs["System"] = {}
        self._System = self._configs["System"]

        # Detector
        self._configs["Detector"] = {}
        self._Detector = self._configs["Detector"]

        # FunctionThreshold
        self._configs["FunctionThreshold"] = {}
        self._FunctionThreshold = self._configs["FunctionThreshold"]

        # GluePathInspection
        self._configs["GluePathInspection"] = {}
        self._GluePathInspection = self._configs["GluePathInspection"]

    @property
    def System(self):
        return self._System

    @property
    def Detector(self):
        return self._Detector

    @property
    def FunctionThreshold(self):
        return self._FunctionThreshold

    @property
    def GluePathInspection(self):
        return self._GluePathInspection

    def update_config(self, new):
        for key in new:
            if key == "System":
                self._System = new['System']
            elif key == "Detector":
                self._Detector = new['Detector']
            elif key == "FunctionThreshold":
                self._FunctionThreshold = new['FunctionThreshold']
            elif key == "GluePathInspection":
                self._GluePathInspection = new['GluePathInspection']

class ServerConfig(object):
    def __init__(self):
        self._configs = {}

        # System
        self._configs["System"] = {}
        self._System = self._configs["System"]

        # Detector
        self._configs["Detector"] = {}
        self._Detector = self._configs["Detector"]

    @property
    def System(self):
        return self._System

    @property
    def Detector(self):
        return self._Detector

    def update_config(self, new):
        for key in new:
            if key == "System":
                self._System = new['System']
            elif key == "Detector":
                self._Detector = new['Detector']

client_config = ClientConfig()
server_config = ServerConfig()