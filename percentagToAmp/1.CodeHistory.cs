using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HkCamer
{
    //============================================================================
    //Date          Author      Version             Item
    //2025/10/14    WC                              1.Add 滑鼠移動關閉上報 S6F1 功能，滑鼠停止60秒後開始恢復上報
    //                                              2.Modidy OverlayWindow 擋主螢幕中間即可  
    //2025/10/27    WC                              1.Issue 修正清除資料，未選擇第一項的問題
    //2025/11/03    WC                              1.Add 派送前將載入資料重置，避免人員使用到上次紀錄的
    //              WC                              2.Add 移除 screenshot 內資料夾，只保留30天
    //              WC                              3.Add 飛靶編號、左掛架、右掛架提供 User 輸入，避免 User 還要再重新手動輸入
    //2025/11/04    WC          1.1.1.32            1.Issue 修正輸入數量後太快執行，導致設定電流為0問題  
    //2025/12/09    WC          1.1.1.33            1.新增自動進出站功能
    //2025/12/29    WC          1.1.1.34            1.由 S6F11 移至 S10F3 才結束 Move Out 忙碌狀態
    //2026/05/07    WC          1.1.1.35            1.讀取 PLC 位置，抓取數值上報至S6F1
    //              WC                              2.修正機台狀態只可加入一次
    //2026/05/11    WC          1.1.1.36            1.修改滑鼠停止30秒後開始恢復上報，並立即上報一次S6F1
    //2026/06/23    WC          1.1.1.37            1.ReadDWithScale 只保留小數點兩位
    //              WC                              2.Add 防呆，以防還在運行中，觸發S6F1
    //2026/07/09    WC          1.1.1.38            1.PLC 連線中斷時，可自動重新連線
    //2026/07/22    WC          1.1.1.39            1.設定失敗(抓取太快，延遲1.5秒)
    //2026/09/XX    WC          1.1.1.40            1.大改版 通訊變更為 MQTT，功能只剩進出站與載入Recipe，其餘功能由PLC自行處理
}
