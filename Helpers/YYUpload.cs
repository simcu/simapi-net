﻿using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace YYApi.Helpers
{
    public class YYUpload
    {
        private string FilePathFolder = "/uploads/";
        public string FilePath
        {
            set
            {
                if (!value.StartsWith("/"))
                {
                    FilePathFolder = $"/{value}";
                }
                if (!FilePathFolder.EndsWith("/"))
                {
                    FilePathFolder += "/";
                }
            }
            get
            {
                return FilePathFolder;
            }
        }

        private IWebHostEnvironment Env { get; }

        public YYUpload(IWebHostEnvironment env)
        {
            Env = env;
        }

        public class YYUploadInfo
        {
            public string Path { get; set; }
            public int Size { get; set; }
        }

        /// <summary>
        /// 保存base64文件,如果有标头按照标头自动识别
        /// </summary>
        /// <param name="base64"></param>
        /// <param name="ext">扩展名</param>
        /// <param name="path">存放路径</param>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        public YYUploadInfo SaveFile(string base64, string ext = null, string path = null, string fileName = null)
        {
            byte[] bt;
            var filePath = FilePath + path;
            if (fileName == null)
            {
                fileName = Guid.NewGuid().ToString();
            }
            var baseArr = base64.Split(";");
            if (baseArr.Length == 2)
            {
                var info = baseArr[0].Split(":")[1].Split("/");
                if (path == null)
                {
                    filePath += $"{info[0]}/";
                }
                if (ext == null)
                {
                    ext = info[1];
                }
                var data = baseArr[1].Split(",");
                bt = Convert.FromBase64String(data[1]);
            }
            else
            {
                bt = Convert.FromBase64String(base64);
            }
            var fn = fileName + (string.IsNullOrEmpty(ext) ? string.Empty : $".{ext}");
            return WriteFile(filePath, fn, bt);
        }

        /// <summary>
        /// 把数据写入文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public YYUploadInfo WriteFile(string filePath, string fileName, byte[] data)
        {
            var realPath = Directory.GetCurrentDirectory() + "/wwwroot" + filePath;
            if (!Directory.Exists(realPath))
            {
                Directory.CreateDirectory(realPath);
            }
            File.WriteAllBytes(realPath + fileName, data);
            return new YYUploadInfo
            {
                Path = filePath + fileName,
                Size = data.Length
            };
        }
    }
}
