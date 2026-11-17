# 创意工坊上传

开发目录保留 Git、源码、工程和本地缓存，每次更新时生成独立的发布目录。`.gitignore` 只控制 Git，不控制 RimWorld 内置上传器。

## 生成发布目录

在仓库根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Prepare-WorkshopUpload.ps1 -ChangeNote "允许钻机放置在逆重飞船底座上"
```

脚本不编译、不提交，也不上传。应先完成需要发布版本的编译和测试，再执行打包。

每次会在 `artifacts/workshop/<唯一编号>/` 下创建：

- `MoreBetterDeepDrill/`：可上传内容，包含 About、LoadFolders.xml，以及加载目录中的 Assemblies、Defs、Languages、Textures、Sounds、Patches。
- `workshop.vdf`：SteamCMD 上传配置，指向上述发布目录和现有工坊条目。
- `manifest.csv`：打包文件列表、大小和 SHA256，供检查。

只打包 `LoadFolders.xml` 为受支持版本启用的目录，包括条件加载的 1.5 PRF 扩展。Assemblies 目录只复制 DLL。`.git`、Source、工程、脚本、文档、构建临时目录和自动生成的 `Assets.zip*` 缓存均不进入发布目录。

每次创建新目录，避免上一次发布中已删除的文件残留。脚本不会移动或删除开发文件。`artifacts` 已被 Git 忽略，旧导出可在上传完成后自行清理。

## 用 SteamCMD 更新现有条目

安装 SteamCMD 后，使用有权更新该工坊条目的 Steam 账号：

```powershell
steamcmd.exe +login 你的Steam登录名 +workshop_build_item "脚本输出的workshop.vdf完整路径" +quit
```

按提示完成登录及 Steam Guard 验证，密码不需要写进脚本。`+workshop_build_item` 会实际上传，请先查看 manifest 和发布目录。

脚本从 `About/PublishedFileId.txt` 读取现有条目 ID，因此更新原条目。VDF 不指定标题、简介、可见性和标签，保留现有值；如需修改它们，应在工坊页面或上传配置中另行处理。

发布目录应保持在 `Mods` 的扫描目录之外。默认输出位于当前模组的 `artifacts` 子目录，不是第二个直接安装的模组；若手动把发布副本放进 `Mods` 下，会与开发版产生重复 packageId。

继续点击开发版的游戏内上传按钮仍会使用开发版根目录。要使用本方案，请通过生成的 VDF 上传。
