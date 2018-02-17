#
# " 1. Find start/End points"
# " 2. Smooth w & a"
# " 3. Find peaks"
# " 4. Use Complimentary filter & Find peaks"
# 
# current_dir <- getwd()

#### NEW FORMAT ####
kitSensorDir <- "E:/SensorStudy/SensorKit/2018-02-13/"#"2018-02-13/"##"C:/SensorKit/2018_01_14/"
kitSensorFile <- "Log2018-02-13_11_43_30_Kevin29CarveMedium.txt" #"Log2018-01-14_13_40_31_CM_16Carve.txt"#"Log2018-02-13_11_43_30_Kevin29CarveMedium.txt" #Log2018-02-13_11_55_02_Kevin42Carve.txt" #"Log2018-02-13_11_36_35_Kevin20CarveFlat.txt"#"Log2018-01-14_13_40_54_RightLeg_16Carve.txt"#"2018-01-05_14_11_34_Kevin24RailroadsTopPeak9.txt"#2018-01-05_14_20_49_Kevin10straightStopx2StraightLong.txt"#"Log2018-01-14_13_40_54_RightLeg_16Carve.txt"#"2018-01-05_14_20_49_Kevin10straightStopx2StraightLong.txt"####
sourceFile <- paste(kitSensorDir, kitSensorFile, sep="")

dat <- read.table(sourceFile, header = FALSE, skip=35,sep="\t",stringsAsFactors=FALSE) #43
dat2 <- dat[dat$V1=="A", ]                 #Get only A rows 
dat2$V4 <- gsub(" received", "", dat2$V3)  #remove Received
df <- ReadNewFormatFile(dat2, debug_print = 1)

# 3.values conversion 
#   wx, wy, wz *  0.001 * PI / 180.0 = radians/second (ang. Velocity)
#   ax, at, az = * 0.001 * 9.81 = m/s^2 (acceleration)

df$wx <- df$wx *  0.001 * pi / 180.0
df$wy <- df$wy *  0.001 * pi / 180.0
df$wz <- df$wz *  0.001 * pi / 180.0
df$ax <- df$ax * 0.001 * 9.81
df$ay <- df$ay * 0.001 * 9.81
df$az <- df$az * 0.001 * 9.81

  # orientation = 1; // left
  # orientation = 2; // up
  # orientation = 3; // down
  # orientation = 4; // right
  # orientation = 5; // top
  # orientation = 6; // bottom
  # }else{
  #   orientation = 0;
  # }

df_stats <- ShowSummary (df)

PlotInitialCharts (df, kitSensorFile)

# up above is all in ETL_SensorKitLoadFile.R file

lag       <- 30
threshold <- 1.5
influence <- 0.5

res<-SmoothAndFindPeaks(df$magnitudeW,df$magnitudeA,lag,threshold,influence)
print (paste("Total peaks in A = ", length(res$pks_a), " peaks in W = ", length(res$pks_w) ))


# X axes
par(mfcol = c(3,2),oma = c(2,2,0,0) + 0.1,mar = c(1,1,1,1) + 0.5)
maxscale <- max(wx,o)
plot(1:length(wx),wx,type="l",col="red",ylim=c(min(wx),maxscale)) 
title(paste("Wx:",kitSensorFile), cex.main = 1,   col.main= "blue")
lines(1:length(o),o,type="l",col="cyan",lwd=2.5)
#Show pks on the chart


# Y axes
maxscale <- max(wy,o)
plot(1:length(wy),wy,type="l",ylab="Wy",xlab="T fraction",col="green",ylim=c(min(wy),maxscale) ) #,main=paste("W:",kitSensorFile))
title(paste("Wy:",kitSensorFile), cex.main = 1,   col.main= "blue")
lines(1:length(o),o,type="l",col="cyan",lwd=2.5)

# Z axes
maxscale <- max(wz,o)
plot(1:length(wz),wz,type="l",ylab="",xlab="",col="blue",ylim=c(min(wz),maxscale)) 
title(paste("Wz:",kitSensorFile), cex.main = 1,   col.main= "blue")
lines(1:length(o),o,type="l",col="cyan",lwd=2.5)

# Accelleration
maxscale <- max(ax,o)
plot(1:length(ax),ax,type="l",ylab="",xlab="",col="red") #,main=paste("W:",kitSensorFile))
title(paste("Ax:",kitSensorFile), cex.main = 1,   col.main= "blue",ylim=c(min(ax),maxscale))
lines(1:length(o),o,type="l",col="cyan",lwd=2.5)

maxscale <- max(ay,o)
plot(1:length(ay),ay,type="l",ylab="",xlab="",col="green",ylim=c(min(ay),maxscale)) #,main=paste("W:",kitSensorFile))
title(paste("Ay:",kitSensorFile), cex.main = 1,   col.main= "blue")
lines(1:length(o),o,type="l",col="cyan",lwd=2.5)

maxscale <- max(az,o)
plot(1:length(az),az,type="l",ylab="",xlab="",col="blue",ylim=c(min(az),maxscale)) #,main=paste("W:",kitSensorFile))
title(paste("Az:",kitSensorFile), cex.main = 1,   col.main= "blue")
lines(1:length(o),o,type="l",col="cyan",lwd=2.5)


# 
# Find start of the turning 
#

summary(df$wx)
summary (df$ax)
summary(df$ay)
summary(df$az)

mean(df$ax) 
mean(df$ay)
mean(df$az)
sd(df$ax)
sd(df$ay)
sd(df$az)


#df2 <- cor(df$wx, df$ax)
q.20=quantile(df$ax,0.20)
q.85=quantile(df$ax,0.85)
df$ax[(df$ax[]>=q.20)&((df$ax[]<=q.85))]

# get start and end of the turning in data
start = ceiling(0.2*nrow(df))
end = ceiling(0.9*nrow(df))
start_row <- min(which(df$ax[1:start] > 0.4)) #W[start_row-1:start_row+1] #W[291:293]

# get the min value from the tail of the dataset
W.s <- dataset2[end:nrow(dataset2), 1:3]

#rum[order(rum$I1, rev(rum$I2), decreasing = TRUE), ] # sort by 1st col desc, 2nd col asc
W.s <- W.s[order(W.s$T, decreasing = TRUE), ] #not sure it's correct reverse sorting by time
minlast_row <- min(which(W.s$W > 0.4))
TRUE_minlast_row <- nrow(dataset2) - minlast_row #W[TRUE_minlast_row:TRUE_minlast_row+1]

# W <- W[start_row:TRUE_minlast_row]
# A <- A[start_row:TRUE_minlast_row]
# 
# symplot =
#   function(x)
#   {
#     n = length(x)
#     n2 = n %/% 2
#     sx = sort(x)
#     mx = median(x)
#     plot(mx - sx[1:n2], rev(sx)[1:n2] - mx,
#          xlab = "Distance Below Median",
#          ylab = "Distance Above Median")
#     abline(a = 0, b = 1, lty = "dotted")
#   }
# symplot(df$ax)


peaks <- findPeaks (resultA$avgFilter)

plot(1:length(az),az,type="l",ylab="",xlab="",col="blue",ylim=c(min(az),maxscale)) #,main=paste("W:",kitSensorFile))
title(paste("Az:",kitSensorFile), cex.main = 1,   col.main= "blue")
lines(1:length(o),o,type="l",col="cyan",lwd=2.5,ylim=c(min(az),maxscale))
lines(1:length(resultA$avgFilter) )

plot(az[peaks], beside = TRUE,
        col = "red", #c("lightblue", "mistyrose", "lightcyan", "lavender", "cornsilk"),
        legend.text = rownames(df_stats), ylim = c(0, 80))

title(main = "Testing Peaks", font.main = 4)

points(az[peaks],col="red",cex=1.2, pch=20)
#points(x[peaks$i], peaks$y.hat[peaks$i], col="Red", pch=19, cex=1.25)
points(peaks, az[peaks], col="Red", pch=19, cex=1.25)

lag       <- 30
threshold <- 1.5
influence <- 0.3

result<- ThresholdingAlgoAll (lag, threshold,influence,kitSensorFile, df)
  

df$magnitudeW <- sqrt(df$wx^2 + df$wy^2 + df$wz^2)
df$magnitudeA <- sqrt(df$ax^2 + df$ay^2 + df$az^2)

resultWm <- ThresholdingAlgo(df$magnitudeW[startRow:endRow],lag,threshold,influence)
resultAm <- ThresholdingAlgo(df$magnitudeA[startRow:endRow],lag,threshold,influence)

par(mfcol = c(2,1),oma = c(2,2,0,0) + 0.1,mar = c(1,1,1,1) + 0.5)

# 1. df$magnitudeW
PlotThresholdChart (resultWm,paste("Magnitudes: lag=",lag,",thrs=",threshold,",infl=",influence),"green",sourceFile,"MagnitudeW")

# 2. df$magnitudeA
PlotThresholdChart (resultAm,"","red",sourceFile,"MagnitudeA")



ThresholdingWithWandA(lag, threshold,influence,kitSensorFile,"X ",wx,ax)

ThresholdingWithWandA(lag, threshold,influence,kitSensorFile,"Y ",wy,ay)

ThresholdingWithWandA(lag, threshold,influence,kitSensorFile,"Z ",wz,az)
