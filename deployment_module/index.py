# encoding=utf8
"""
    Author:  'jdwang'
    Date:    'create date: 2016-10-08'; 'last updated date: 2016-10-08'
    Email:   '383287471@qq.com'
    Describe: ITPR 规划助理 SLU OOD 检测 和 主处理 模块
"""

import web
import json
import sys
from ood_detection.ood_detection import ood_detecion
sys.path.append('.')
sys.path.append('..')

urls = (
    '/ITPR/MainProcessing/response/rawInput=(.*)&context=(.*)&relatedParam=(.*)&candiate=(.*)', 'MainProcessing',
    '/ITPR/MainProcessing/oodDetection/rawInput=(.*)', 'OODDetection',
)

app = web.application(urls, globals(), False)


class MainProcessing(object):
    def GET(self, rawInput, context, relatedParam, candiate):
        rawInput = rawInput.strip()
        # print rawInput
        if len(rawInput) == 0:
            # 空输入时..
            status = 'False|input is null'
        else:

            status = 'True|OK'

        result = {'rawInput': rawInput,
                  'status': status,
                  }
        web.header('Content-Type', 'application/json')
        result = json.dumps(result)
        return result


class OODDetection(object):
    '''OOD 检测

    '''
    def GET(self, rawInput):
        rawInput = rawInput.strip()
        # print rawInput
        is_ood = False
        if len(rawInput) == 0:
            # 空输入时..
            status = 'False|input is null'
        else:
            status = 'True|OK'
            is_ood = ood_detecion(rawInput)


        result = {'rawInput': rawInput,
                  'status': status,
                  'is_ood': is_ood,
                  }
        web.header('Content-Type', 'application/json')
        result = json.dumps(result)
        return result


if __name__ == "__main__":
    app.run()
