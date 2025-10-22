# Aliyun.OpenAIForward.Net6

阿里云函数中实现OpenAI的转发，支持多目标转发，如openrouter等第三方分发，支持流式输出，仅需部署一次。

## 快速部署

### 创建Web函数
**不要香港**，要新加坡或者其他被支持的海外区域。

![image](https://github.com/user-attachments/assets/e98b990a-80b4-4296-a9d0-bc85e29af603)

### 基础参数

将本项目Release中的zip文件直接上传

![image](https://github.com/user-attachments/assets/bcc646f8-cc83-4097-b255-3686799f070e)

```
dotnet ./Aliyun.OpenAIForward.Net6.dll
```

### 高级参数

![image](https://github.com/user-attachments/assets/60b42bfa-932f-4b8e-a88d-df3445c1e96a)

### 测试是否可用
查看函数的Web触发器，链接大概是下面这样，直接访问，如果下载得到**Hello World!**的文本文件即为部署成功

https://xxxxxxxx.ap-southeast-1.fcapp.run


## 如何使用

**建议绑定自己域名，也可使用公开触发器自带的域名fcapp.run地址，但fcapp.run域名无法支持流式输出！**

如在lobe中，你可以配置这样的代理，其他客户端中同理。

OpenAI代理
```
OPENAI_PROXY_URL=https://xxxxxxxx.ap-southeast-1.fcapp.run/v1
```

OPENROUTER代理
```
OPENROUTER_PROXY_URL=https://xxxxxxxx.ap-southeast-1.fcapp.run/openrouter/v1
```

## 自行设置转发目标
在云函数中设置环境变量**ROUTE_MAPPING**，格式为json格式的key:value结构，key为链接中路径中的第1位，如上面的openrouter，value则为对应其实际URL。

```json
{"openrouter": "https://openrouter.ai/api/"}
```

## 其他问题
如果需要流式，可能需要
