namespace MediaCat.Core.Model {
   
    public sealed class ImportItem {
      
        public string Filepath { get; set; }

        public Mime Mime { get; set; }

        public long Filesize { get; set; }

    }

}