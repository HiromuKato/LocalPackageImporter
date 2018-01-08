Local Package Importer

================================================================================
Description
================================================================================
"Local Package Importer" is an editor extension that lists local unitypackages 
and can import them.

Default local folder is below:
 - Windows : C:/Users/(USERNAME)/AppData/Roaming/Unity\Asset Store-5.x
 - Mac     : /Users/(USERNAME)/Library/Unity/Asset Store-5.x

================================================================================
How to use
================================================================================
Select [Window] - [Local Package Importer]

Heart button at the top : Show only favorite unitypackages if turn on
Search Field : Search unitypackages
Update metadata button：Get or Update unitypackages's metadata

About each unitypackage
 - Import button : Import unitypackage
 - Asset Store button : Show unitypackage information in Asset Store
 - heart button : Set favorite or not

================================================================================
Caution
================================================================================
For the first time, Icons etc. are not showed because metadata of unitypackages
are not held. By pushing "Update metadata" button, metadata are gotten and saved
under the folder described below.

 - Windows : C:/Users/(USERNAME)/Documents/LocalPackageImporter
 - Mac     : /Users/(USERNAME)/Library/LocalPackageImporter

* If you don't need "Local Package Importer", please delete the above folder.

================================================================================
Disclaimer
================================================================================
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

================================================================================
Others
================================================================================
For more information, please visit the website:
https://github.com/HiromuKato/LocalPackageImporter

================================================================================
Version History
================================================================================
Version 1.0.3
 - Improved performance when pushed [Update metadata] button.

Version 1.0.2
 - Updated to draw unitypackage info as much as needed to improve performance.
 - Updated to handle exception when pushed [Update metadata] button.
 - Changed tmp path.

Version 1.0.1
 - Fixed an issue that an error occurred when there was no icon in unitypackage.
 - Added [Open unitypackage Folder] menu.

Version 1.0.0
 - First Release.
