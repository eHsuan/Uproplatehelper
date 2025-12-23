using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HkCamer
{
    //============================================================================
    //Date          Author      Version             Item
    //2025/10/14    WC                              1.Add 滑鼠移動關閉上報 S6F11 功能，滑鼠停止60秒後開始恢復上報
    //                                              2.Modidy OverlayWindow 擋主螢幕中間即可  
    //2025/10/27    WC                              1.Issue 修正清除資料，未選擇第一項的問題
    //2025/11/03    WC                              1.Add 派送前將載入資料重置，避免人員使用到上次紀錄的
    //              WC                              2.Add 移除 screenshot 內資料夾，只保留30天
    //              WC                              3.Add 飛靶編號、左掛架、右掛架提供 User 輸入，避免 User 還要再重新手動輸入
    //2025/11/04    WC          1.1.1.32            1.Issue 修正輸入數量後太快執行，導致設定電流為0問題  
    //2025/12/09    WC          1.1.1.33            1.新增自動進出站功能   
}
