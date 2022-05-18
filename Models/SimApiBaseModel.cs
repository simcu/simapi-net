using System;
using System.Linq;
using System.Text.Json;
using SimApi.Helpers;

namespace SimApi.Models
{
    public class SimApiBaseModel
    {
        protected virtual string[] MapperIgnoreField { get; set; } =
        {
            "Id", "CreatedAt", "UpdatedAt"
        };

        protected virtual string UpdatedTimeField { get; set; } = "UpdatedAt";

        public void MapData<TS>(TS source, bool mapAll = false)
        {
            //获取要赋值的源数据不为null的项目
            var sourceProps = source.GetType().GetProperties().Where(x => x.GetValue(source) != null)
                .Select(x => new
                {
                    x.Name,
                    x.PropertyType
                })
                .ToDictionary(x => x.Name, x => x.PropertyType);

            //获取目标对象的属性信息
            var targetProps = GetType().GetProperties().Select(x => new
                {
                    x.Name,
                    x.PropertyType
                })
                .ToDictionary(x => x.Name, x => x.PropertyType);
            foreach (var sp in sourceProps)
            {
                //检查源对象不为空的属性是否在目标对象中存在
                if (targetProps.ContainsKey(sp.Key) && sp.Value == targetProps[sp.Key] &&
                    (!MapperIgnoreField.Contains(sp.Key) || mapAll))
                {
                    GetType().GetProperty(sp.Key)?
                        .SetValue(this, source.GetType().GetProperty(sp.Key)?.GetValue(source));
                }
            }

            UpdateTime();
        }

        public void MapData<TS>(TS source, string[] mapFields)
        {
            //获取要赋值的源数据不为null的项目
            var sourceProps = source.GetType().GetProperties().Where(x => x.GetValue(source) != null)
                .Select(x => new
                {
                    x.Name,
                    x.PropertyType
                })
                .ToDictionary(x => x.Name, x => x.PropertyType);

            //获取目标对象的属性信息
            var targetProps = GetType().GetProperties().Select(x => new
                {
                    x.Name,
                    x.PropertyType
                })
                .ToDictionary(x => x.Name, x => x.PropertyType);
            foreach (var sp in sourceProps)
            {
                //检查源对象不为空的属性是否在目标对象中存在
                if (targetProps.ContainsKey(sp.Key) && sp.Value == targetProps[sp.Key] &&
                    mapFields.Contains(sp.Key))
                {
                    GetType().GetProperty(sp.Key)?
                        .SetValue(this, source.GetType().GetProperty(sp.Key)?.GetValue(source));
                }
            }

            UpdateTime();
        }

        /// <summary>
        /// 更新update时间
        /// </summary>
        /// <returns></returns>
        public void UpdateTime()
        {
            GetType().GetProperty(UpdatedTimeField)?.SetValue(this, SimApiUtil.CstNow);
        }
    }
}