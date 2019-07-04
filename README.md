### Lolipop:The easier SQLite database engine for CSharp

##### # Getting started

###### Setting up the library

The first step is to import this dependency into your project.Please import this lib on Project Resource Manager.

###### Add config for your application

The second is to open your app.config on "(your project name)/App.config" and insert a node like this:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    ...
    <appSettings>
        <!--The value is your sqlite database location-->
        <add key="DBFilePath" value="./test.db"/>
    </appSettings>
</configuration>
```

If you encounter such an error:

`Hybrid mode assemblies are generated for v2.0.50727 runtime and cannot be loaded at 4.0 runtime without configuration of other information.`

You should modify your app.config like this:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <!--Add 'useLegacyV2RuntimeActivationPolicy="true"' in startup node-->

    <startup useLegacyV2RuntimeActivationPolicy="true"> 

        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.6.1" />

        <supportedRuntime version="v2.0.50727"/>

    </startup>
    ...
    ...
</configuration>
```

After that configuration process is completed.

##### #UsAge

- Make your model extends LolipopDataHelper, like this:

```csharp
// Lolipop can extend your model to SQL operations
public class Student: LolipopDataHelper
{

    ...
    ...
}
```

- Also you can nominate a different file to save your data,like this:

```csharp
public class MyTime: LolipopDataHelper
{
    // base("d://test.db"),'d://test.db' can be your file path

    public MyTime() : base("d://test.db")

    {

        ...

    }

    ...
    ...

}
```

- If you want to add Field constraints for your Model Fields, you can do it like this:

```csharp
// add 'PRIMARY KEY' constraints for Field named 'StuID'
[LolipopColumnAttr("PRIMARY KEY")]
 public int StuID { get; set; }
```

After that, the model name(Class Name) will be your SQLite Database Tables name and Fields name in the model will be your Database Tables Fields name.

- The full example like this:

```csharp
public class Student: LolipopDataHelper
{

    [LolipopColumnAttr("PRIMARY KEY")]

    public int StuID { get; set; }

    public int StuGrade { get; set; }

    public bool StuSex { get; set; }

    public string StuName { get; set; }

    public string StuClass { get; set; }


    /**

     *    if you want nominate a different file to save your data

     *    public Student():base("d://test.db")

     *    {

     *       ...

     *    }

     */

    public override string ToString()

    {

        return string.Format("学生号：{0}\t年段：{1}\t性别：{2}\t姓名：{3}\t班级：{4}\t", StuID, StuGrade, StuSex, StuName, StuClass);

    }

}
```

- Your Database Table will be like this:

- ![avatar](./screenshot/screen-01.png)
