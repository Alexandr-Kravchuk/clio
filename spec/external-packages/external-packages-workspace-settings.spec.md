вот контент файла workspaceSettings.json, который должен быть в корне репозитория

'''
{
  "Packages": [
    "CrtFinservAppMgmt",
    "CrtFinservAppMgmtApp",
    "CrtFinservAppMgmtObjMdl",
    "CrtFinservAppPrms",
    "CrtFinservAppMgmtAI",
    "CrtFinservAppMgmtSamples",
    "CrtFinservAccMgmt",
    "CrtFinservAccMgmtAI",
    "CrtFinservAccMgmtApp",
    "CrtFinservAccMgmtObjMdl",
    "CrtFinservBankCoreAPI",
    "CrtFinservCommonInputUtils",
    "CrtFinservCstmr360",
    "CrtFinservCstmr360App",
    "CrtFinservCstmr360ObjMdl",
    "CrtFinservPrdctMgmt",
    "CrtFinservPrdctMgmtApp",
    "CrtFinservPrdctMgmtObjMdl",
    "CrtFinservSalesMgmt",
    "CrtFinservSalesMgmtApp",
    "CrtFinservSalesMgmtObjMdl",
    "CrtFinservSalesMgmtInMarketing",
    "CrtFinservAppMgmtInSales"
  ],
  "ExternalPackages": [
    "CommonModule"
  ],
  "IgnorePackages": [
    "CrtCustomer360"
  ],
  "ApplicationVersion": "1.0.0"
}
'''

command publish-app должна работать так
в zip должны быть помещены:
- пакеты, указанные в Packages
- внешние пакеты, указанные в ExternalPackages
- пакеты, указанные в IgnorePackages, не должны быть включены в zip
- зависимости внешних пакетов, указанных в ExternalPackages, должны быть включены в zip если они есть в папке ../packages и их нет в IgnorePackages
- при этом команда pushw и restorew не должна скачивать или загружать на сервер пакеты из ExternalPackages
- свойство IgnorePackages (уже существует в коде) используется для исключения пакетов из команд pushw, restorew и publish