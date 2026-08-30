# 迁移与恢复

## 日常恢复点

- 小步提交到功能分支；`main` 只保留已评审的稳定状态。
- 可试玩里程碑创建带注释标签，例如：`git tag -a prototype-v0.1.0 -m "First playable"`。
- 标签不是外部备份；里程碑还应推送远端并创建 Git bundle。

## 创建离线完整备份

在仓库根目录执行：

```powershell
./Tools/New-GitBundleBackup.ps1
```

脚本仅备份已提交的完整 Git 历史，默认输出到项目容器下的 `_Backups/FunGameProject/`，并自动校验。若工作区不干净，脚本会停止，避免漏掉未提交文件。

## 在另一台电脑恢复

```powershell
git clone <bundle-file> FunGameProject
cd FunGameProject
git switch main
```

随后用 `ProjectSettings/ProjectVersion.txt` 指定的 Unity 版本打开目录。`Library`、`Temp` 等生成内容不迁移，由 Unity 重建。

## 回档原则

- 查看旧版本优先使用新分支：`git switch -c recovery/<说明> <tag-or-commit>`。
- 不默认使用 `git reset --hard`，因为它会破坏未提交工作。
- 二进制资源增多后启用 Git LFS，并同步更新迁移检查清单。

## GitHub 接入清单

1. 确认仓库名称、所属账号/组织及公开性。
2. 创建空仓库，不自动生成 README 或 `.gitignore`。
3. 添加 `origin`，首次推送 `main` 并验证远端提交。
4. 为稳定分支启用保护规则（如适用）。
5. 验证从 GitHub 和最新 bundle 均可恢复。

