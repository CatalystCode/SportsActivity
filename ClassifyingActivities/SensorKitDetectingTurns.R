#SensorKit_DetectingTurns
##
# " 1. Read the file"
# " 2. Parse the file, get df with 10 columns"
# " 3. Apply calculations for each w & a columns"
# " 4. Find start/End points"
# " 5. Smooth w & a"
# " 6. Find peaks"
# 
# current_dir <- getwd()

ReadNewFormatFile <- function(df, debug_print = 0) {
  ##
  ## debug_print values: 0 - don't print, 1 - print totals, 2 - print all
  ## 
  first_comma_pos_next_line <- -1
  pos <- -1
  comma_cnt <- -1
  full_line <- "" # 
  parsed_set_line_i <- data.frame(T =as.POSIXct(character()), V2 = character(0))
  parsed_set <- data.frame(T =as.POSIXct(character()), V2 = character(0))
  
  if (debug_print > 0) {print (paste("Total rows =", nrow(df) ))}
  
  for(i in 1:nrow(df))
  {
    if (nchar(full_line) == 0) {
      #initial setup
      start <- 1
      full_line <- df$V4[i]
      parsed_set_line_i <- as.data.frame(rbind(c(df$V2[i], full_line)))
      
    } else{
      #count comma separators in full line
      comma_cnt <- sum(unlist(strsplit(full_line,"")) == ",")
      new_line_exists <- sum(unlist(strsplit(full_line,"")) == "\n")
      if (new_line_exists > 0) {pos <- regexpr(pattern ='\n',full_line)[1]}
      len_full_line <- nchar(full_line)
      first_comma_pos_next_line <- regexpr(pattern =',',df$V4[i])[1]
      
      # one line of data could be presented in one, 2 or 3 lines
      # when received '\n' - process line, or when next line has time fraction field(length = 8)
      if (
        (new_line_exists > 0  && pos == len_full_line) | 
        (first_comma_pos_next_line == 8 && len_full_line > 10) 
      ) 
      { 
        full_line <- gsub("\n", "", full_line)
        if (nchar(full_line)> 0) {
          parsed_set_line_i$V2 <- full_line                 #update full combined string
          parsed_set <- rbind(parsed_set,parsed_set_line_i) #save into resulting dataset
          
          #parse only full lines with 7 commas & 8 numbers - got all the columns
          if (comma_cnt==7) {
            s <- strsplit(full_line, ",")
            xx <- unique(unlist(s)[grepl('[A-Z]', unlist(s))])
            sap <- t(sapply(seq(s), function(i){
              wh <- which(!xx %in% s[[i]]); n <- suppressWarnings(as.numeric(s[[i]]))
              nn <- n[!is.na(n)]; if(length(wh)){ append(nn, NA, wh-1) } else { nn }
            })) 
            dt <- data.frame(parsed_set_line_i,sap)
            if (ncol(dt)==10) {
              #save data only when all columns are present, ignore lines with missing fields
              if (exists('dt_result') && is.data.frame(get('dt_result'))) {
                dt_result <-rbind(dt_result,dt) # need plyr to fill out missing values with NA
              }
              else{
                dt_result <- dt
              }
            }
          }
          if (debug_print==2 & (exists('dt_result') && is.data.frame(get('dt_result')))) {
            print(paste("SAVED: i=",i,"start=",start," full_line=",full_line,
                        " rows=",nrow(parsed_set)," parsed rows=",nrow(dt_result)))}
        }
        
        #reset values
        comma_cnt <- 0
        start <- 1
        pos <- -1
        
        full_line <- gsub(" ", "", df$V4[i]) 
        parsed_set_line_i <- as.data.frame(rbind(c(df$V2[i], full_line)))   #save next row in temp dataframe
        
      } else{
        #get next chunk, append and remove extra spaces
        full_line <- gsub(" ", "", paste(full_line, df$V4[i], sep="")) 
        start <- start + 1            
      }
    }
  }
  col_names <- c('t','line_to_parse',"t_fraction",'wx', 'wy', 'wz', 'ax', 'ay', 'az', 'o')
  colnames(dt_result) <- col_names            #rename columns
  
  if (debug_print>0) {print (paste("Rows read =", nrow(df), ", processed =", nrow(parsed_set), 
                                   ", parsed =", nrow(dt_result)))}
  return(dt_result)
}

PlotSingleChart <- function(gf,chart_title, mainColor, fileName, sidetext,newPlot=FALSE){
  
  if (newPlot) {
    plot(1:length(gf),gf,type="l",ylab="",xlab="",col=mainColor, lwd = 2 ) 
    abline(h = 0, v = 500 * c(1:9), lty = 2, lwd = .2, col = "gray70")
  }else{
    lines(1:length(gf),gf,type="l",col=mainColor,lwd=2)
  }
  if (chart_title !="") {title(paste(chart_title,":",fileName), cex.main = 1,   col.main= "blue")}
  mtext(sidetext,side=4,col="blue",cex=1)
  
}

PlotInitialCharts <- function(df, fileName){
  ###############
  #
  # Plot initial observations for Angular Velocity  & Accelleration 
  # 
  ###############
  par(mfcol = c(3,2),oma = c(2,2,0,0) + 0.1,mar = c(1, 1, 1, 1) + 0.5)
  
  PlotSingleChart(df$wx,"Angular Velocity ","red", fileName,"Wx",TRUE)
  PlotSingleChart(df$wy,"","green", fileName,"Wy",TRUE)
  PlotSingleChart(df$wz,"","blue", fileName,"Wz",TRUE)
  
  PlotSingleChart(df$ax,"Accelleration ","red", fileName,"Ax",TRUE)
  PlotSingleChart(df$ay,"","green", fileName,"Ay",TRUE)
  PlotSingleChart(df$az,"","blue", fileName,"Az",TRUE)
}

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
#
# a - accelleration
# w - angular velocity (gyro)
#
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
  
  # Find peaks
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
  
  #remove index with smalest value from close peaks A - work from end to start for indexes to stay valid
  for (i in rev(check_a)){
    if (a_smooth2$avgFilter[pks_a[i]] < a_smooth2$avgFilter[pks_a[i+1]]){
      remove_index<-i
    }else{remove_index<-i+1}
    pks_a<-pks_a[-remove_index]
  }
  #remove index with smalest value from close peaks W
  for (i in rev(check_w)){
    if (w_smooth2$avgFilter[pks_w[i]] < w_smooth2$avgFilter[pks_w[i+1]]){
      #remove index with smalest value from pks_w
      remove_index<-i
    }else{remove_index<-i+1}
    pks_w<-pks_w[-remove_index]
  }
  
  return(list("pks_a"=pks_a,"pks_w"=pks_w,"a_smooth"=a_smooth2$avgFilter,"w_smooth"=w_smooth2$avgFilter,"a_stdFilter"=a_smooth2$stdFilter,"w_stdFilter"=w_smooth2$stdFilter))
}

#####################

# " 1. Read the file"
kitSensorDir <- "C:/test/SensorKit/Data/"
kitSensorFile <- "2018-01-14_13_40_31_CM_16Carve.txt"
sourceFile <- paste(kitSensorDir, kitSensorFile, sep="")

dat <- read.table(sourceFile, header = FALSE, skip=35,sep="\t",stringsAsFactors=FALSE) #43
dat2 <- dat[dat$V1=="A", ]                 #Get only A rows 
dat2$V4 <- gsub(" received", "", dat2$V3)  #remove Received

# " 2. Parse the file, get df with 10 columns"
df <- ReadNewFormatFile(dat2, debug_print = 0)

# " 3. Apply calculations for each w & a columns"
#   wx, wy, wz *  0.001 * PI / 180.0 = radians/second (ang. Velocity)
#   ax, at, az = * 0.001 * 9.81 = m/s^2 (acceleration)
df$wx <- df$wx *  0.001 * pi / 180.0
df$wy <- df$wy *  0.001 * pi / 180.0
df$wz <- df$wz *  0.001 * pi / 180.0
df$ax <- df$ax * 0.001 * 9.81
df$ay <- df$ay * 0.001 * 9.81
df$az <- df$az * 0.001 * 9.81

#opar <- par()      # make a copy of current settings

PlotInitialCharts (df, kitSensorFile)

# Calculate magnitude for W & A
df$magnitudeW <- sqrt(df$wx^2 + df$wy^2 + df$wz^2)
df$magnitudeA <- sqrt(df$ax^2 + df$ay^2 + df$az^2)

# " 5. Smooth w & a"
# " 6. Find peaks"
lag       <- 30
threshold <- 1.5
influence <- 0.5

res<-SmoothAndFindPeaks(df$magnitudeW,df$magnitudeA,lag,threshold,influence)
print (paste("Total peaks in A = ", length(res$pks_a), " peaks in W = ", length(res$pks_w) ))

par(mfcol = c(2,1),oma = c(2,2,0,0) + 0.1,mar = c(1,1,1,1) + 0.5)

PlotSingleChart(res$a_smooth,"Magnitude Acc. Smooth 2","blueviolet", kitSensorFile,"MagnitudeA",TRUE) 
abline(h = c(1:round(max(res$a_smooth,na.rm=T))), v = 500 * c(1:round(length(res$a_smooth)/500)), lty = 2, lwd = .2, col = "gray70")
points(res$pks_a, res$a_smooth[res$pks_a], col="Red", pch=19, cex=1.0)

PlotSingleChart(res$w_smooth,"Magnitude Gyro Smooth 2","cyan4", kitSensorFile,"MagnitudeW",TRUE) 
abline(h = 0.5 * c(1:round(max(res$w_smooth,na.rm=T)/0.5)), v = 500 * c(1:round(length(res$w_smooth)/500)), lty = 2, lwd = .2, col = "gray70")
points(res$pks_w, res$w_smooth[res$pks_w], col="Red", pch=19, cex=1.0)

####################