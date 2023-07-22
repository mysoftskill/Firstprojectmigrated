DECLARE @startDate varchar(max) = '2014-11-11'
DECLARE @endDate varchar(max) = '2014-11-12'
DECLARE @dateFormat varchar(max) = 'yyyy-MM-dd HH:00'

              SELECT FORMAT(Timestamp, 'yyyy-MM-dd') AS Timestamp
                ,OperationName
                ,CAST((CAST(SUM([Success]) AS numeric)/SUM([Total])) AS decimal(8,7)) AS Qos
              FROM MemberViewAnalytics.dbo.MemberViewQos soh (NOLOCK)
              WHERE BaseType = 'IncomingServiceRequest' AND Timestamp BETWEEN @startDate AND CONCAT(@endDate, ' 23:59')
              GROUP BY FORMAT(Timestamp, 'yyyy-MM-dd'), OperationName 