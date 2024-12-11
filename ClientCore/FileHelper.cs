using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore
{
    public static class FileHelper
    {
        public static void CopyDirectory(string sourceDirPath, string saveDirPath)
        {
            if (!string.IsNullOrEmpty(sourceDirPath) && Directory.Exists(sourceDirPath))
            {
                if (!Directory.Exists(saveDirPath))
                {
                    Directory.CreateDirectory(saveDirPath);
                }

                string[] files = Directory.GetFiles(sourceDirPath);
                foreach (string file in files)
                {
                    string pFilePath = Path.Combine(saveDirPath, Path.GetFileName(file));
                    // 不会把map文件和ini复制到根目录，不然会影响任务包或MOD判定。
                    string extension = Path.GetExtension(file);
                    if (extension != ".map" && extension != ".png" && extension != ".ini" || Path.GetFileName(file).StartsWith("uimd") )
                    {
                        try
                        {
                            if(File.Exists(pFilePath))
                            {
                                File.SetAttributes(pFilePath, FileAttributes.Normal);
                                File.Delete(pFilePath);
                            }
                                
                            File.Copy(file, pFilePath, true);
                        }
                        catch
                        {
                            throw new FileLockedException($"文件操作失败，可能是这个文件{file}被占用了");
                        }
                    }
                }
            }
        }

        public static void DelFiles(List<string> deleteFiles)
        {
            if (deleteFiles != null)
            {
                foreach (string file in deleteFiles)
                {
                    try
                    {   if(!string.IsNullOrEmpty(file))
                            File.Delete(file);
                    }
                    catch
                    {
                        throw new FileLockedException($"文件操作失败，可能是这个文件{file}被占用了");
                    }
                }
            }
        }
    }
}


