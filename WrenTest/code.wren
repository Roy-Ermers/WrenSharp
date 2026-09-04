import "engine" for Vector3, logger

var vector1 = Vector3.new(1,2,3)
var vector2 = Vector3.new()


logger.log(vector1.print())
logger.error("This went wrong")
System.print(vector2.print())