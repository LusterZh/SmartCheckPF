# -*- coding:utf-8 -*-
""" Example of json-rpc usage with Wergzeug and requests.
NOTE: there are no Werkzeug and requests in dependencies of json-rpc.
NOTE: server handles all url paths the same way (there are no different urls).
# https://pypi.org/project/json-rpc/
"""
import _init_Odyssey
from werkzeug.wrappers import Request, Response
from werkzeug.serving import run_simple
from jsonrpc import JSONRPCResponseManager, dispatcher
from function.client import InitialSegModel, start, stop, add_model, release_model, inference, release_SegModel
#from function.function import cut_image
import time
import socket

@Request.application
def application(request):
    dispatcher["start"] = lambda client_param_list, reg_param_str_list, golden_sample_list:start(client_param_list, reg_param_str_list, golden_sample_list)
    dispatcher["stop"] = lambda i:stopPython(i)
    dispatcher["InitialSegModel"] = lambda model_root:InitialSegModel(model_root)
    dispatcher["add_model"] = lambda function_name, roi, model_root:add_model(function_name, roi, model_root)
    dispatcher["release_model"] = lambda function_name:release_model(function_name)
    dispatcher["release_SegModel"] = lambda i:stopSeg(i)
    dispatcher["inference"] = lambda img_path, function_list, stat:inference(img_path, function_list, stat)
    
   # dispatcher["cut_image"] = lambda img_path:cut_image(img_path)
   
    response = JSONRPCResponseManager.handle(
        request.get_data(cache=False, as_text=True), dispatcher)
    return Response(response.json, mimetype='application/json')


def stopPython(i):
    result = stop()
    return result

def stopSeg(i):
    release_SegModel()
    return True



if __name__ == '__main__':
    #myname = socket.getfqdn(socket.gethostname())
    myname = socket.gethostname()
    myaddr = socket.gethostbyname(myname)
    print ("myname:"+myname+"  myaddr: "+myaddr)
    #run_simple(myaddr, 4002, application)
    run_simple("0.0.0.0", 4002, application)


