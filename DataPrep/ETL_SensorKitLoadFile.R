#
# " 1. Read the file"
# " 2. Parse the file, get df with 10 columns"
# " 3. Apply calculations for each w & a columns"
# " 4. Get charts for w & a"

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
          if (debug_print==2) {print(paste("SAVED: i=",i,"start=",start," full_line=",full_line,
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
    plot(1:length(gf),gf,type="l",ylab="",xlab="",col=mainColor, lty = ) #,axes=F,useRaster=F) # bty='n') 
    abline(h = 0, v = 500 * c(1:9), lty = 2, lwd = .2, col = "gray70")
  }else{
    lines(1:length(gf),gf,type="l",col=mainColor,lwd=2)
  }
  
  #plot(r,axes=F,useRaster=F)
  if (chart_title !="") {title(paste(chart_title,":",fileName), cex.main = 1,   col.main= "blue")}
  #abline(h=mean(gf),col="cyan",lty = 6, lwd = 1)
  mtext(sidetext,side=4,col="blue",cex=1)
}

PlotInitialCharts <- function(df, fileName){
  ###############
  #
  # 4. Plot initial observations for Angular Velocity  & Accelleration 
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

#### NEW FORMAT ####
kitSensorDir <- "C:/SensorKit/2018_01_14/"
kitSensorFile <- "Log2018-01-14_13_40_31_CM_16Carve.txt"
sourceFile <- paste(kitSensorDir, kitSensorFile, sep="")

dat <- read.table(sourceFile, header = FALSE, skip=43, sep="\t",stringsAsFactors=FALSE) #
dat2 <- dat[dat$V1=="A", ]                 #Get only A rows 
dat2$V4 <- gsub(" received", "", dat2$V3)  #remove word "received"

df = ReadNewFormatFile(dat2, debug_print = 1)

# 3.values conversion 
#   wx, wy, wz *  0.001 * PI / 180.0 = radians/second (ang. Velocity)
#   ax, at, az = * 0.001 * 9.81 = m/s^2 (acceleration)
df$wx <- df$wx *  0.001 * pi / 180.0
df$wy <- df$wy *  0.001 * pi / 180.0
df$wz <- df$wz *  0.001 * pi / 180.0
df$ax <- df$ax * 0.001 * 9.81
df$ay <- df$ay * 0.001 * 9.81
df$az <- df$az * 0.001 * 9.81

PlotInitialCharts (df, kitSensorFile)



