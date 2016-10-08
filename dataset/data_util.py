# encoding=utf8
"""
    Author:  'jdwang'
    Date:    'create date: 2016-10-08'; 'last updated date: 2016-10-08'
    Email:   '383287471@qq.com'
    Describe:
"""
from __future__ import print_function
import io
import json

class DataUtil(object):
    def get_topn_sight(self,topn=10):
        """获取 top n个热门评分高的景点

        :param topn: int
            top n 个 景点
        :return:
        """
        with io.open('indomain/主处理数据集.json','r',encoding='utf8') as fin:
            sights = json.load(fin)
            # print(len(sights))
            topn_sights = sorted(sights.itervalues(),key=lambda x:x['sight_rating'])[-topn:]
            topn_sight_names = ['%s,%s'%(item['sight_name'],item['sight_rating']) for item in topn_sights]
            print('\n'.join(topn_sight_names))




if __name__ == '__main__':
    data_util = DataUtil()
    data_util.get_topn_sight(10)