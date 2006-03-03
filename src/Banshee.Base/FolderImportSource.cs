/***************************************************************************
 *  FolderImportSource.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using Mono.Unix;
using Gtk;

namespace Banshee.Base
{
    public class FolderImportSource : IImportSource
    {
        private static FolderImportSource instance;
        public static FolderImportSource Instance {
            get {
                if(instance == null) {
                    instance = new FolderImportSource();
                }
                
                return instance;
            }
        }
        
        private FolderImportSource()
        {
        }
    
        public void Import()
        {
            FileChooserDialog chooser = new FileChooserDialog(
                Catalog.GetString("Import Folder to Library"),
                null,
                FileChooserAction.SelectFolder
            );
            
            try {
                 chooser.SetCurrentFolderUri(Globals.Configuration.Get(GConfKeys.LastFileSelectorUri) as string);
            } catch(Exception) {
                 chooser.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            }
            
            chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton(Stock.Open, ResponseType.Ok);
            chooser.DefaultResponse = ResponseType.Ok;
            
            if(chooser.Run() == (int)ResponseType.Ok) { 
                ImportManager.Instance.QueueSource(chooser.Uri);
            }
            
            Globals.Configuration.Set(GConfKeys.LastFileSelectorUri, chooser.CurrentFolderUri);
            
            chooser.Destroy();
        }
        
        public string Name {
            get { return Catalog.GetString("Local Folder"); }
        }
        
        private Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(22, Stock.Open);
        public Gdk.Pixbuf Icon {
            get { return icon; }
        }
    }
}
