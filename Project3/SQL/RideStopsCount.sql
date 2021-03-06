/****** Script for SelectTopNRows command from SSMS  ******/
USE [retdbim]
GO

-- aantal stops gegroepeerd per uur van alle 90 dagen

SELECT 	 
	DATEPART(HOUR, [Time]) as 'hour',
	COUNT(*) AS CountRides
  FROM [RideStops]
  GROUP BY DATEPART(HOUR, [Time])
  ORDER BY 'hour';

SELECT 	 
	DATEPART(HOUR, [Time]) as 'hour',
	DATEPART(WEEKDAY, [Time]) as 'Day_Of_Week',
	COUNT(*) AS CountRides
  FROM [RideStops]
  GROUP BY DATEPART(HOUR, [Time]), DATEPART(WEEKDAY, [Time])
  ORDER BY 'hour' , 'Day_Of_Week';

