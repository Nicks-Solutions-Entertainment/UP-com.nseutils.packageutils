# UP-com.nseutils.packageutils
A Unity Custom Pakage for manage dependencies of another external custom packages such as Git packages that depends of another Git package per exemple.

 This pakcage was maded for a easy solution to Unity "limitation" on manage Git Packages dependencies on `package.json` files.

To usse this Package to auto-install external package dependencies of your custom package, just put on root of yout package folder a file with name of 'coremodulesettings.json'  with a structurte like this:
```
{
    "dependencies" : [
        {
            "packageName" : "com.your-team.your-package",
            "githubUrl" : "https://github.com/Your-Team/your-package.git",
            "version" : "x.x.x"
        },
       {
            "packageName" : "com.your-team.your-other-package",
            "githubUrl" : "https://github.com/Your-Team/your-other-package.git",
            "version" : "x.x.x-pre.x"
        },
       {
            "packageName" : "com.your-team.your-aother-package",
            "githubUrl" : "https://github.com/Your-Team/your-aother-package.git",
            "version" : "x.x.x-beta.x"
        },
    ]
}
```


The version must to be on Siemantic pattern to work with the Module installer script.
