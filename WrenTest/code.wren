import "engine" for Vector3, logger

var vector1 = Vector3.new(1,2,3)
var vector2 = Vector3.new()


logger.log(vector1.toString())
System.print(vector1.x)
vector1.x = 3
System.print(vector1.x)

System.print(vector1.toString())
