#
# " 1. Find start/End points"
# " 2. Smooth w & a"
# " 3. Find peaks"
# " 4. Use Complimentary filter & Find peaks"
# 
# current_dir <- getwd()

# up above is all in ETL_SensorKitLoadFile.R file

SlidingAvg<-function(y,lag,threshold,influence){
  
  filteredY <- y[0:lag]
  avgFilter <- NULL
  stdFilter <- NULL
  avgFilter[lag] <- mean(y[0:lag])
  stdFilter[lag] <- sd(y[0:lag])
  
  #1-st sliding average
  for (i in (lag+1):length(y)){
    if (abs(y[i]-avgFilter[i-1]) > threshold*stdFilter[i-1]) {
      filteredY[i] <- influence*y[i]+(1-influence)*filteredY[i-1]
    } else {
      filteredY[i] <- y[i]
    }
    avgFilter[i] <- mean(filteredY[(i-lag):i])
    stdFilter[i] <- sd(filteredY[(i-lag):i])
  }
  return(list("avgFilter"=avgFilter,"stdFilter"=stdFilter))
}

SmoothAndFindPeaks <- function(w,a,lag,threshold,influence) {
  
  a_smooth1 <- SlidingAvg(a,lag,threshold,influence)
  st<-which(a_smooth1$stdFilter >0.1* threshold* a_smooth1$avgFilter)
  startPos<-min(st)
  endPos<-max(st)
  
  par(mfcol = c(1,1),oma = c(2,2,0,0) + 0.1,mar = c(1,1,1,1) + 0.5)
  PlotSingleChart(a_smooth1$avgFilter,"Magnitude Accelleration activity start/end","blueviolet", kitSensorFile,"MagnitudeA",TRUE) 
  abline(h = 0.5 * c(1:round(max(a_smooth1$avgFilter,na.rm=T)/0.5)), v = 200 * c(1:round(length(a_smooth1$a_smooth)/200)), lty = 2, lwd = .2, col = "gray70")
  points(st, a_smooth1$avgFilter[st], col="red", pch=19, cex=1.25)
  
  print (paste("Calculation for activity start =", min(st), " end=",max(st) ))
  
  #2-nd averaging
  a_smooth2<- SlidingAvg(a_smooth1$avgFilter[lag:length(a_smooth1$avgFilter)],lag,threshold,influence )
  w_smooth1<-SlidingAvg(w,lag,threshold,influence )
  w_smooth2<-SlidingAvg(w_smooth1$avgFilter[lag:length(w_smooth1$avgFilter)],lag,threshold,influence )
  
  a_signals <- rep(0,length(a_smooth2$avgFilter))
  w_signals <- rep(0,length(w_smooth2$avgFilter))
  
  pks_a <- numeric(0)
  pks_w <- numeric(0) 
  
  for (i in startPos:endPos){
    #difference & sign (if it's raising 1 or falling -1 ) 
    a_signals[i-1] <- sign(a_smooth2$avgFilter[i]-a_smooth2$avgFilter[i-1])
    w_signals[i-1] <- sign(w_smooth2$avgFilter[i]-w_smooth2$avgFilter[i-1])
  }
  for (i in startPos:endPos){
    #get differense of signs
    a_signals[i-1] <- a_signals[i]-a_signals[i-1]
    w_signals[i-1] <- w_signals[i]-w_signals[i-1]
  }
  
  #when difference is negative - peak, sign changes from 1 to -1
  pks_a <-which(a_signals<0)
  pks_w <-which(w_signals<0)
  
  #remove peaks that are too close
  check_a <-which(diff(pks_a)<12)
  check_w <-which(diff(pks_w)<12)
  
  #remove index with smalest value from closed peaks A
  for (i in rev(check_a)){
    if (a_smooth2$avgFilter[pks_a[i]] < a_smooth2$avgFilter[pks_a[i+1]]){
      remove_index<-i
    }else{remove_index<-i+1}
    pks_a<-pks_a[-remove_index]
  }
  #remove index with smalest value from closed peaks W
  for (i in rev(check_w)){
    if (w_smooth2$avgFilter[pks_w[i]] < w_smooth2$avgFilter[pks_w[i+1]]){
      #remove index with smalest value from pks_w
      remove_index<-i
    }else{remove_index<-i+1}
    pks_w<-pks_w[-remove_index]
  }
  
  return(list("pks_a"=pks_a,"pks_w"=pks_w,"a_smooth"=a_smooth2$avgFilter,"w_smooth"=w_smooth2$avgFilter,"a_stdFilter"=a_smooth2$stdFilter,"w_stdFilter"=w_smooth2$stdFilter))
}

lag       <- 30
threshold <- 1.5
influence <- 0.5

res<-SmoothAndFindPeaks(df$magnitudeW,df$magnitudeA,lag,threshold,influence)
print (paste("Total peaks in A = ", length(res$pks_a), " peaks in W = ", length(res$pks_w) ))

par(mfcol = c(2,1),oma = c(2,2,0,0) + 0.1,mar = c(1,1,1,1) + 0.5)

PlotSingleChart(res$a_smooth,"Magnitude Accelleration AVG 2","blueviolet", kitSensorFile,"MagnitudeA",TRUE) 
abline(h = c(1:round(max(res$a_smooth,na.rm=T))), v = 500 * c(1:round(length(res$a_smooth)/500)), lty = 2, lwd = .2, col = "gray70")
points(res$pks_a, res$a_smooth[res$pks_a], col="Red", pch=19, cex=1.0)

PlotSingleChart(res$w_smooth,"Magnitude Gyro AVG 2","cyan4", kitSensorFile,"MagnitudeW",TRUE) 
abline(h = 0.5 * c(1:round(max(res$w_smooth,na.rm=T)/0.5)), v = 500 * c(1:round(length(res$w_smooth)/500)), lty = 2, lwd = .2, col = "gray70")
points(res$pks_w, res$w_smooth[res$pks_w], col="Red", pch=19, cex=1.0)

####################