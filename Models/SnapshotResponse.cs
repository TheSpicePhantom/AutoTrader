namespace AutoTrader.Models
{
    public record Snapshot(SR_Response Response, SR_OpenPosition[] open_positions);
    public record SR_Response(bool Executed);
    public record SR_OpenPosition(       // TEST_DATA \/ ------------------------- \/ THAT ONE ALWAYS VISIBLE TRADE                                            
        int     t,                       // t: 1                                 "t": 1,
        int     ratePrecision,           // ratePrecision: 2                     "ratePrecision": 0,
        string  tradeId,                 // tradeId: "181116987"                 "tradeId": "",
        string  accountName,             // accountName: "01333834"              "accountName": "",
        string  accountId,               // accountId: "1333834"                 "accountId": "",
        double  roll,                    // roll: -29.32                         "roll": -58.64,
        double  com,                     // com: 0                               "com": 0,
        double  open,                    // open: 4077.78                        "open": 0,
        object  valueDate,               // valueDate: ""                        "valueDate": "",
        double  grossPL,                 // grossPL: -142.20865                  "grossPL": -285.65059,
        double  close,                   // close: 4062.79                       "close": 0,
        double  visiblePL,               // visiblePL: -149.9                    "visiblePL": -301.1,
        bool    isDisabled,              // isDisabled: False                    "isDisabled": false,
        string  currency,                // currency: "SPX500"                   "currency": "SPX500",
        bool    isBuy,                   // isBuy: True                          "isBuy": false,
        double  amountK,                 // amountK: 0.01                        "amountK": 0.02,
        double  currencyPoint,           // currencyPoint: 0.00095               "currencyPoint": 0,
        string? time,                    // time: "12012022173348"               "time": null,
        double  usedMargin,              // usedMargin: 1126.25                  "usedMargin": 0,
        string? orderId,                 // orderId: "396611705"                 "orderId": null,
        string? OpenOrderRequestTXT,     // OpenOrderRequestTXT: "test 1"        "openOrderRequestTXT": null,
        string? OpenOrderReqID,          // OpenOrderReqID: "Request-295360490"  "openOrderReqID": null,
        object? TradeIDOrigin,           // TradeIDOrigin: null                  "tradeIDOrigin": null,
        string? child_trailingStop,      // child_trailingStop: "none"           "child_trailingStop": null,
        int?    child_trailing,          // child_trailing: -1                   "child_trailing": null,
        int     stop,                    // stop: 0                              "stop": 0,
        int     stopMove,                // stopMove: 0                          "stopMove": 0,
        int     limit                    // limit: 0                             "limit": 0
    );                                                                           
    
    public class SnapshotResponse
    {
        public IEnumerable<SR_OpenPosition> OpenPositions { get; set; }
        public string Message { get; set; }

        public SnapshotResponse(Snapshot? snapshot = null, string message = "")
        {
            OpenPositions = snapshot != null ? snapshot.open_positions.Where(op => !string.IsNullOrWhiteSpace(op.tradeId)) : new List<SR_OpenPosition>();
            Message = message;
        }
    }
}
