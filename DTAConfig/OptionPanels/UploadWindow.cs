using ClientCore;
using ClientCore.Entity;
using ClientCore.Settings;
using ClientGUI;
using DTAConfig.Entity;
using Localization.Tools;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XNATextBox = ClientGUI.XNATextBox;

namespace DTAConfig.OptionPanels
{
    /// <summary>
    /// 上传
    /// </summary>
    public class UploadWindow(WindowManager windowManager,Component component = null) : XNAWindow(windowManager)
    {

        private const int CtbW = 130;
        private const int CtbH = 25;

        private XNATextBox _ctbName;
        private XNATextBox _ctbGameOptions;
        private XNATextBox _ctbAuthor;
        private XNATextBox _ctbVersion;
        private XNATextBox _ctbTags;
        private XNAClientButton btnSelect;
        private XNAClientButton btnSelectOther;
        private XNAClientTabControl tabControl;
        private List<string> types = ["地图", "任务包", "Mod","地图包","其他"];

        public bool Uploaded = false;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 550, 420);
          
            base.Initialize();
            CenterOnParent();

            tabControl = new XNAClientTabControl(WindowManager)
            {
                Name = "tabControl",
                ClientRectangle = new Rectangle(12, 40, 0, 0),
                ClickSound = new EnhancedSoundEffect("button.wav"),
                FontIndex = 1,
            };

            foreach (var item in types) tabControl.AddTab(item, UIDesignConstants.BUTTON_WIDTH_133);

            tabControl.SelectedIndexChanged += Tab切换事件;
            tabControl.MakeUnselectable(1);
            tabControl.MakeUnselectable(2);

            AddChild(tabControl);

            var _lblTitle = new XNALabel(WindowManager)
            {
                ClientRectangle = new Rectangle(230, 10, 0, 0),
            };
            AddChild(_lblTitle);

            //第一行
            var lblName = new XNALabel(WindowManager)
            {
                Text = "组件名称:",
                ClientRectangle = new Rectangle(20, 90, 0, 0)

            };
            AddChild(lblName);

            _ctbName = new XNATextBox(WindowManager)
            {
                ClientRectangle = new Rectangle(lblName.Right + 80, lblName.Y, CtbW, CtbH)
            };
            AddChild(_ctbName);

            //第二行
            var lblGameOptions = new XNALabel(WindowManager)
            {
                Text = "Tag:",
                ClientRectangle = new Rectangle(lblName.X, lblName.Y + 40, 0, 0)
            };
            AddChild(lblGameOptions);


            _ctbGameOptions = new XNATextBox(WindowManager)
            {
                ClientRectangle = new Rectangle(_ctbName.X, lblGameOptions.Y, 440, CtbH)
            };
            AddChild(_ctbGameOptions);

            //第三行
            var lblAuthor = new XNALabel(WindowManager)
            {
                Text = "作者:",
                ClientRectangle = new Rectangle(lblName.X, lblGameOptions.Y + 40, 0, 0)
            };
            AddChild(lblAuthor);

            _ctbAuthor = new XNATextBox(WindowManager)
            {
                ClientRectangle = new Rectangle(_ctbName.X, lblAuthor.Y, CtbW, CtbH)
            };
            AddChild(_ctbAuthor);

            var lblVersion = new XNALabel(WindowManager)
            {
                Text = "版本：",
                ClientRectangle = new Rectangle(300, lblAuthor.Y, 0, 0),
            };
            AddChild(lblVersion);

            _ctbVersion = new XNATextBox(WindowManager)
            {
                ClientRectangle = new Rectangle(lblVersion.X + 50, lblVersion.Y, CtbW, CtbH)
            };
            AddChild(_ctbVersion);

            //第四行
            var lblDescription = new XNALabel(WindowManager)
            {
                Text = "地图介绍:",
                ClientRectangle = new Rectangle(lblName.X, lblVersion.Y + 40, 0, 0)
            };
            AddChild(lblDescription);

            _ctbTags = new XNATextBox(windowManager)
            {
                ClientRectangle = new Rectangle(_ctbAuthor.X, lblDescription.Y, _ctbGameOptions.Width, CtbH)
            };

            AddChild(_ctbTags);

            //第五行
            btnSelect = new XNAClientButton(WindowManager)
            {
                Text = "点击选择以上传:",
                X = _ctbTags.X,
                Y = 260,
                Width = _ctbGameOptions.Width,
                IdleTexture = AssetLoader.LoadTexture("Resources\\ThemeDefault\\160pxtab.png"),
                HoverTexture = AssetLoader.LoadTexture("Resources\\ThemeDefault\\160pxtab_c.png"),
            };
            btnSelect.LeftClick += BtnSelect_LeftClick;
            AddChild(btnSelect);

            btnSelectOther = new XNAClientButton(WindowManager)
            {
                Text = "点击选择以上传附带的文件(如CSF等):",
                X = _ctbTags.X,
                Y = btnSelect.Y + 40,
                Width = _ctbGameOptions.Width,
                IdleTexture = AssetLoader.LoadTexture("Resources\\ThemeDefault\\160pxtab.png"),
                HoverTexture = AssetLoader.LoadTexture("Resources\\ThemeDefault\\160pxtab_c.png"),
            };
            btnSelectOther.LeftClick += btnSelectOther_LeftClick;
            AddChild(btnSelectOther);

            var btnUpload = new XNAClientButton(WindowManager)
            {
                Text = "上传",
                ClientRectangle = new Rectangle(150, 360, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
            };
            
            AddChild(btnUpload);

            var btnCancel = new XNAClientButton(WindowManager)
            {
                Text = "取消",
                ClientRectangle = new Rectangle(310, 360, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
            };
            AddChild(btnCancel);

            btnCancel.LeftClick += (_, _) =>
            {
                Disable();
            };

            _ctbAuthor.Text = UserINISettings.Instance.User.username;
            if (component != null)
            {
                _ctbName.Text = component.name;
                _ctbGameOptions.Text = component.tags;
                //_ddType.Text = component.typeName;
                _ctbAuthor.Text = component.author;
                _ctbTags.Text = component.description;
                _ctbVersion.Text = component.version;
                tabControl.SelectedTab = types.IndexOf(component.typeName);
                tabControl.Enabled = false;
                _lblTitle.Text = "编辑组件";
                btnUpload.Text = "修改";
                btnUpload.LeftClick += 编辑;
            }
            else
            {
                _lblTitle.Text = "上传组件";
                btnUpload.Text = "上传";
                btnUpload.LeftClick += 上传;
            }

        }

        private void Tab切换事件(object sender, EventArgs e)
        {
            
            switch (tabControl.SelectedTab)
            {
                case 0:
                    切换到地图类型();
                    break;
                case 1:
                    切换到任务包类型();
                    break;
                case 2:
                    切换到Mod类型();
                    break;
                case 3:
                    切换到地图包类型();
                    break;
                default:
                    切换到地图类型();
                    break;
            }
        }

        private void 切换到Mod类型()
        {
            
        }

        private void 切换到任务包类型()
        {
            
        }

        private void 切换到地图包类型()
        {
            btnSelectOther.Visible = false;
            
        }

        private void 切换到地图类型()
        {
            btnSelectOther.Visible = true;
        }

        private string[] OtherFiles = [];
        private string[] MapFiles = [];


        private void btnSelectOther_LeftClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "",
                Title = "选择附带的文件",
                Multiselect = true // 允许多选
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            // 使用 List<string> 保存选中的文件路径
            OtherFiles = openFileDialog.FileNames;

            // 提取文件名并使用逗号分隔
            string fileNames = string.Join(", ", Array.ConvertAll(OtherFiles, Path.GetFileName));

            // 设置显示的最大长度
            int maxLength = 50; // 根据需要调整最大长度
            if (fileNames.Length > maxLength)
            {
                fileNames = string.Concat(fileNames.AsSpan(0, maxLength), "...");
            }

            btnSelectOther.Text = fileNames;
        }

        private void 编辑(object sender, EventArgs e)
        {
            var error = Check(false);
            if (error != null)
            {
                XNAMessageBox.Show(WindowManager, "错误", error);
                return;
            }

            var path = 检查地图并打包(btnSelect.Text);

            var componentDTO = new MultipartFormDataContent
                    {
                        { new StringContent(component.id.ToString(), encoding: null), "id" },
                        { new StringContent(_ctbName.Text, encoding: null), "name" },
                        { new StringContent(_ctbAuthor.Text, encoding: null),"author"},
                        { new StringContent(_ctbTags.Text, encoding: null),"description"},
                        { new StringContent(_ctbGameOptions.Text, encoding: null),"tags"},
                        { new StringContent(tabControl.SelectedTab.ToString(), encoding: null),"type"},
                        { new StringContent(_ctbVersion.Text, encoding: null),"version"},
                        { new StringContent(UserINISettings.Instance.User.id.ToString(), encoding: null),"uploadUser"},
                   //     { fileContent, "file", Path.GetFileName(path) }
                    };

            if (path != string.Empty)
            {

                if (double.Parse(new FileInfo(path).Length.ToFileSizeString(1)) > 5)
                {
                    XNAMessageBox.Show(WindowManager, "错误", "地图不能超过5MB");
                    return;
                }

                using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                var fileContent = new StreamContent(fileStream);
                componentDTO.Add(fileContent, "file", Path.GetFileName(path));

                var (r, msg) = NetWorkINISettings.Post<bool?>("component/updComponent", componentDTO).GetAwaiter().GetResult();

                if (r != true)
                {
                    XNAMessageBox.Show(WindowManager, "错误", msg);
                    return;
                }
            }
            else
            {
                var (r, msg) = NetWorkINISettings.Post<bool?>("component/updComponent", componentDTO).GetAwaiter().GetResult();

                if (r != true)
                {
                    XNAMessageBox.Show(WindowManager, "错误", msg);
                    return;
                }
            }
            XNAMessageBox.Show(WindowManager, "信息", " 修改成功！");
            Uploaded = true;
            Disable();

        }

        private void 上传(object sender, EventArgs e)
        {
            // 上传
            var error = Check();
            if (error != null)
            {
                XNAMessageBox.Show(WindowManager, "错误", error);
                return;
            }

            var path = 检查地图并打包(btnSelect.Text);

            if(path.Length == 0)
            {
                XNAMessageBox.Show(WindowManager, "错误", "请选择地图文件");
                return;
            }

            if(double.Parse(new FileInfo(path).Length.ToFileSizeString(1)) > 500)
            {
                XNAMessageBox.Show(WindowManager, "错误", "压缩包不能超过500MB");
                return;
            }

            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var fileContent = new StreamContent(fileStream);

            var componentDTO = new MultipartFormDataContent
                    {
                        { new StringContent(_ctbName.Text, encoding: null), "name" },
              //          { new StringContent(apply, encoding: null), "apply" },
                        { new StringContent(_ctbAuthor.Text, encoding: null),"author"},
                        { new StringContent(_ctbTags.Text, encoding: null),"description"},
                        { new StringContent(_ctbGameOptions.Text, encoding: null),"tags"},
                        { new StringContent(tabControl.SelectedTab.ToString(), encoding: null),"type"},
                        { new StringContent(_ctbVersion.Text, encoding: null),"version"},
                        { new StringContent(UserINISettings.Instance.User.id.ToString(), encoding: null),"uploadUser"},
                        { fileContent, "file", Path.GetFileName(path) }
                    };
            WindowManager.progress.Report("正在上传任务包...");
           var (r,msg) = NetWorkINISettings.Post<bool?>("component/addComponent", componentDTO).GetAwaiter().GetResult();

            if (r != true)
            {
                XNAMessageBox.Show(WindowManager, "错误", msg);
                return;
            }
            try
            {
                File.Delete(path);
            }
            catch(Exception ex)
            {
                Logger.Log($"文件{path}删除失败:{ex.Message}");
            }
            XNAMessageBox.Show(WindowManager, "信息", "上传成功！");
            Uploaded = true;
            Disable(); 
            WindowManager.progress.Report("");

        }

        private string 检查地图并打包(string selectedFile)
        {
            string extension = Path.GetExtension(selectedFile).ToLower();


            if (tabControl.SelectedTab == 0)
            {
                #region 打包地图
                // 如果是 .yrm、.mpr 或 .map 文件，压缩成 .7z 文件
                if (extension == ".yrm" || extension == ".mpr" || extension == ".map")
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var originalFileName = Path.GetFileNameWithoutExtension(selectedFile);
                    var newFileName = $"{originalFileName}_{timestamp}{extension}";
                    var directoryPath = $"{originalFileName}_{timestamp}";

                    if (!Directory.Exists($"./tmp/{directoryPath}/{directoryPath}"))
                        Directory.CreateDirectory($"./tmp/{directoryPath}/{directoryPath}");

                    // 复制并重命名文件
                    File.Copy(selectedFile, Path.Combine($"./tmp/{directoryPath}/", newFileName), true);

                    foreach (var file in OtherFiles)
                    {
                        File.Copy(file, $"./tmp/{directoryPath}/{directoryPath}/{Path.GetFileName(file)}", true);
                    }

                    string compressedFile = Path.Combine(
                        $"./tmp/",
                        $"{directoryPath}.7z"
                    );

                    SevenZip.CompressWith7Zip($"./tmp/{directoryPath}/*", compressedFile);
                    try
                    {
                        Directory.Delete($"./tmp/{directoryPath}", true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"目录./tmp/{directoryPath}删除失败:{ex.Message}");
                    }
                    return compressedFile;
                }
                else
                    return string.Empty;
                #endregion
            }
            else
            {
                #region 打包多地图

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

                var directoryPath = $"地图包_{timestamp}";

                if (!Directory.Exists($"./tmp/{directoryPath}"))
                    Directory.CreateDirectory($"./tmp/{directoryPath}");


                foreach (var file in MapFiles)
                {
                    File.Copy(file, $"./tmp/{directoryPath}/{Path.GetFileName(file)}", true);
                }

                string compressedFile = Path.Combine(
                    $"./tmp/",
                    $"{directoryPath}.7z"
                );

                SevenZip.CompressWith7Zip($"./tmp/{directoryPath}/*", compressedFile);
                try
                {
                    Directory.Delete($"./tmp/{directoryPath}", true);
                }
                catch (Exception ex)
                {
                    Logger.Log($"目录./tmp/{directoryPath}删除失败:{ex.Message}");
                }
                return compressedFile;
                #endregion
            }

        }

        private void BtnSelect_LeftClick(object sender, EventArgs e)
        {

            if (tabControl.SelectedTab == 0) //地图
            {
                #region 选择地图
                // 打开文件对话框选择地图
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Map Files (*.yrm;*.mpr;*.map)|*.yrm;*.mpr;*.map",
                    Title = "选择地图文件"
                };

                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string selectedFile = openFileDialog.FileName;


                btnSelect.Text = selectedFile;


                // 自动设置 _ctbName 文本框的值
                if (string.IsNullOrEmpty(_ctbName.Text))
                {
                    _ctbName.Text = Path.GetFileNameWithoutExtension(btnSelect.Text);
                }
                #endregion
            }
            else
            {
                #region 选择多地图
                // 打开文件对话框选择多个地图文件
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Map Files (*.yrm;*.mpr;*.map)|*.yrm;*.mpr;*.map",
                    Title = "选择多个地图文件",
                    Multiselect = true // 允许多选
                };

                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                // 将符合类型的文件路径保存到 MapFiles 列表
                MapFiles = openFileDialog.FileNames;

                // 显示文件数在按钮上
                btnSelect.Text = $"已选择 {MapFiles.Length} 个文件";
                #endregion
            }


        }

        private string Check(bool upLoad = true)
        {
            if( _ctbName.Text.Length == 0 || _ctbAuthor.Text.Length == 0 || _ctbVersion.Text.Length == 0)
            {
                return "存在未填写的信息。";
            }

            //if (!File.Exists(btnSelect.Text) && upLoad)
            //{
            //    return $"文件{btnSelect.Text}不存在！";
            //}

            return null;
        }
    }
}
