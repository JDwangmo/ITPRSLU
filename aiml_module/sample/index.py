#encoding=utf8
import web
import json
import logging
import sys
sys.path.append('.')
sys.path.append('..')
# print sys.path
from aiml_module import aiml

urls = (
    '/aiml/rawInput=(.*)&context=(.*)&relatedParam=(.*)&candiate=(.*)', 'AimlProcess'
)

app = web.application(urls, globals())


k = aiml.Kernel()
# 注意当前目录是在  跟aiml文件夹所在的目录
k.learn('cn-startup.xml')

# Use the 'respond' method to compute the response
# to a user's input string.  respond() returns
# the interpreter's response, which in this case
# we ignore.
k.respond("load aiml cn")

# Loop forever, reading user input from the command
# line and printing responses.

class AimlProcess:
    def GET(self, rawInput,context,relatedParam,candiate):
        rawInput = rawInput.strip()
        # print rawInput
        # 空输入时..
        if len(rawInput)==0:
            status = 'False|input is null'

        aimlresponse = k.respond(rawInput)

        understandTemplate=''

        if len(aimlresponse) == 0:
            status = 'False|No match found for input'
        else:
            understandTemplate = '|'.join(aimlresponse.split('|')[:2])
            status = 'True'
        result = {'rawInput':rawInput,
                  'aimlResult':aimlresponse,
                  'understandTemplate':understandTemplate,
                  'status':status
                  }
        result = json.dumps(result)
        web.header('Content-Type','application/json')
        return result


if __name__ == "__main__":
    app.run()