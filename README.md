Для запуска необходимо установить VisualStudio 2022+ с поддержкой .NET 9
Перед запуском необходимо указать путь к файлу CSV в файле Appsetting в разделе train
В файле CSV необходимо заменить названия столбцов в первой строчке на следующие: OrderId	OrderTimestamp	DistanceInMeters	DurationInSeconds	TenderId	TenderTimestamp	DriverId	DriverRegDate	DriverRatingString	Carmodel	Carname	Platform	PickupInMeters	PickupInSeconds	UserId	PriceStartLocal	PriceBidLocal	IsDone
