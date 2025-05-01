using System;

namespace N_m3u8DL_CLI.GUI.Models
{
    /// <summary>
    /// 表示分页导航中的一个页码
    /// </summary>
    public class PageNumber
    {
        /// <summary>
        /// 页码数字
        /// </summary>
        public int Number { get; set; }
        
        /// <summary>
        /// 是否为当前页
        /// </summary>
        public bool IsCurrentPage { get; set; }
        
        /// <summary>
        /// 是否为省略号
        /// </summary>
        public bool IsEllipsis { get; set; }
    }
}
