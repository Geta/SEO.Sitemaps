#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see http://aka.ms/containercompat 

FROM microsoft/aspnet:4.7.2-windowsservercore-1803

RUN Add-WindowsFeature Web-WebSockets

ARG source
WORKDIR /inetpub/wwwroot
COPY ${source:-obj/Docker/publish} .
WORKDIR /
RUN MKDIR data
WORKDIR data
RUN MKDIR ClientResources
RUN MKDIR Views
WORKDIR /inetpub/wwwroot

RUN New-WebVirtualDirectory -Site 'Default Web Site' -Name 'Episerver/Geta.SEO.Sitemaps/' -PhysicalPath c:\data\


