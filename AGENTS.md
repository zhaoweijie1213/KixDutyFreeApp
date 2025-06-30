# 代码风格

- 使用 `dotnet format` 保证格式；命名遵循 **PascalCase**（类型/方法）和 **_camelCase**（私有字段）。
- 不允许使用 `dynamic`，除非原因写进代码注释。
- 每个实体字段和方法必须要写注释.，所有注释使用简体中文,除非是专业术语
- API返回结果必须定义强类型，不能是object，必须有明确的返回结构
- 实体的主键不能是nullable类型
- int 、 long 等代表数量的类型不能是nullable类型

## 构建 & 测试
1. `dotnet restore`
2. `dotnet build -c Release /warnaserror`

> 所有补丁必须在本地通过以上 2 步，且行覆盖率 **≥ 80%**。

## 质量检查

先检测是否所有方法和属性都有注释,如果没有则补充完善,然后进行质量检查

```bash
dotnet format --verify-no-changes
dotnet tool run dotnet-reportgenerator
```

## README更新

质量检查完成后，根据本次更新内容完善项目根目录的README.md文件,附上本次更新时间(北京时间) yyyy-MM-dd HH:mm:ss





