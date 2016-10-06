# encoding=utf8
"""
    Author:  'jdwang'
    Date:    'create date: 2016-10-06'; 'last updated date: 2016-10-06'
    Email:   '383287471@qq.com'
    Describe: 数据处理工具，包括 将简单数据格式 转为 AIML格式的 数据格式
"""
from __future__ import print_function
import os,io


class DataUtil(object):
    def parse_to_AIML_file(self, rules_dir, aiml_dir):
        """将简单的规则数据格式转为AIML格式的数据格式

        :param rules_dir: 简单规则文件夹
        :param aiml_dir: AIML文件夹
        :return:
        """
        files = os.listdir(rules_dir)
        for file_name in files:
            in_file_path = os.path.join(rules_dir, file_name)
            out_file_path = os.path.join(aiml_dir, file_name.replace('txt', 'aiml'))

            with io.open(out_file_path, 'w',encoding='utf8') as fout:
                fout.write(u'<?xml version="1.0" encoding="utf-8"?>\n')
                fout.write(u'<aiml>\n')
                template = None
                for line in io.open(in_file_path,encoding='utf8'):
                    if line.startswith('<template>'):
                        template = line.strip().replace('<template>', '')
                    elif line.startswith('<pattern>'):

                        pattern = line.strip().replace('<pattern>', '')
                        pattern = ' '.join([item for item in pattern])
                        fout.write(u'<category>\n')
                        fout.write(u'<pattern>%s</pattern>\n' % pattern)
                        fout.write(u'<template>\n')
                        fout.write(u'<random>\n')
                        fout.write(u'<li>%s</li>\n' % template)
                        fout.write(u'</random>\n')
                        fout.write(u'</template>\n')
                        fout.write(u'</category>\n')

                fout.write(u'</aiml>')


if __name__ == '__main__':
    data_util = DataUtil()
    data_util.parse_to_AIML_file(
        rules_dir='./ITPR_indomain/rules',
        aiml_dir='./ITPR_indomain/aimls'
    )
