/goal



I used to have a Windows program (with MSI installer) written in VB to print bar codes named "IBE Barcode", it has a standard version and a professional version (with more options). Unfortunately I lost the source code of the project. It used to be sold online at www.ibebarcode.com and various other places. You can see old marketing materials and other related documents in docs sub-folder.



The ibebarcode.com web site has source code at: C:\\GitHub\\Migrate\\httpd\\ibebarcodecom



Let's recreate this program, with a new name "IBE Barcode Generator" and make it free, code available on GitHub, and has all the functions of the old Professional Version program, plus:



1. Runs on multiple platforms, either an installable/executable for Windows/Linux(different flavors)/Mac, or as a web site that runs at ibebarcode.com (which will eventually be hosted at a free Azure account)

2\. Both executable and web site version should share the same code and both should reside inside a single solution file. Working folder: C:\\GitHub\\IBEGroup\\IBEBarcodeGeneratror

3\. Should run on latest .NET 10 or .NET 10 Core project

4\. Add all commonly used barcodes, especially QR codes

5\. We should not need any database connection

6\. Should support localization, initially we need to support English, simplified Chinese and traditional Chinese, Japanese

7\. MIT license



Plan first, ask me if not clear.



