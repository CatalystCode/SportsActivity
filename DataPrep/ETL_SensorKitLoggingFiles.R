ReadOldFormatFile <- function(readDir, readFile) {

  sourceFile <- paste(readDir, readFile, sep="")
  #read the file as tab separated, skip first 35 lines
  dat <- read.table(sourceFile, header = FALSE, skip=35, sep="\t")
  dat2 <- dat[dat$V1=="A", ]  #Get only A rows
  
  ### OLD FORMAT Start: timestamp, W - angular Velocity magnitude, A - accelleration magnitude
  dat2$V4 <- gsub(" received", "", dat2$V3)  #remove Received
  dat2$V4 <- gsub("W:", "", dat2$V4)         #remove W:
  dat2$V4 <- gsub("A:", "", dat2$V4)         #remove A:
  dat2$V4 <- gsub("\n", "", dat2$V4)         #remove end of line /n
  
  #-- Split V3 column by comma
  s <- strsplit(dat2$V4, ",")
  xx <- unique(unlist(s)[grepl('[A-Z]', unlist(s))])
  sap <- t(sapply(seq(s), function(i){
    wh <- which(!xx %in% s[[i]]); n <- suppressWarnings(as.numeric(s[[i]]))
    nn <- n[!is.na(n)]; if(length(wh)){ append(nn, NA, wh-1) } else { nn }
  })) 
  #--
  
  #dat2$W <- data.frame(do.call('rbind', strsplit(as.character(dat2$V4),',',fixed=FALSE)))
  #dataset2 <- cbind.data.frame(dat2$V2,dat2$X1, dat2$X2)
  
  ### OLD FORMAT End
  
  dataset2 <- data.frame(dat2[2:3], sap)     #take columns 2, 3, and 
  col_names <- c('T','LineToParse','W','A')
  colnames(dataset2) <- col_names            #rename columns
  
  return(dataset2)
}


kitSensorFile <- "E:/SensorStudy/SensorKit/2018_01_04/2018-01-04_11_26_20_StraightLinePizzaNoCarvingToSuperConnectSlow.txt"

kitSensorDir <- "SourceData/"
kitSensorFile <- "2018-01-05_14_11_34_Kevin24RailroadsTopPeak9.txt"

dataset2 <- ReadOldFormatFile(kitSensorDir, kitSensorFile)

# NEW format
# remove word "RECEIVED" & empty rows
dat2$V4 <- gsub(" received", "", dat2$V3)
dat2remove <- dat2[dat2$V4=="\n",] #just to check number of rows to be removed
dat3 <- dat2[dat2$V4!="\n",] # clean dat2, no empty rows
nrow(dat3)  # NROW(na.omit(dat3))

ShortLine = dat2[grep("\n", dat2$V3), ]

nrow(ShortLine)

sum(dat2$V3 == "\n")
row(dat2[dat2$V3 == "\n",])
length(dat2remove$V3[dat2remove$V3 == "\n"])
sum(which(dat2$V3 == "\n"))

dat3[length(dat3$V4) < 5,]
filter(dat3, dat3$V4.Length < 3)

#split into 2 dataframes with start & end of the string, assuming there are 2 rows to conbine into 1
dat4 = dat3[seq(1, nrow(dat3), 2), ] 
dat5 = dat3[seq(2, nrow(dat3), 2), ]

#remove spaces
dat4$V4 <- gsub(" ", "", dat4$V4)
dat5$V4 <- gsub(" ", "", dat5$V4)

#combine full string
dat6 <- cbind.data.frame(Time1=dat4$V2,Time2=dat5$V2,startStr=dat4$V4,endstr=dat5$V4, fullstr=paste(dat4$V4, dat5$V4, sep=""))
dat6$fullstr <- gsub("\n", "", dat6$fullstr)

dat7 <- data.frame(do.call('rbind', strsplit(as.character(dat6$fullstr),',',fixed=TRUE)))

