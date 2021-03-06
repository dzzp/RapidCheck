#include "tracking_utils.h"
#include "similarity_utils.h"
#include <time.h>
using namespace cv;
using namespace rc;

/**
	calculate and compare similarity between two tracklets
*/
void compareSimilarity()
{
	// set input video
	VideoCapture cap(filepath);

	// initialize colors	
	vector<Scalar> colors = getRandomColors();
	
	// build target detected frames
	vector<Frame> frames, framePedestrians, frameCars;
	clock_t t = clock();
	if (SELECT_DETECTION_RESPONSE)
	{
		readTargets(cap, framePedestrians, frameCars);
	}
	else
	{
		detectTargets(cap, frames);
	}
	if (USE_PEDESTRIANS_ONLY)
	{
		frames = framePedestrians;
	}
	else
	{
		frames = frameCars;
	}
	t = clock() - t;
	if (DEBUG)
		printf("Detection takes %d(ms)\n", t);

	// build all tracklets
	vector<Segment> segments;
	t = clock();
	int classId = USE_PEDESTRIANS_ONLY ? 1 : 0;
	buildTracklets(frames, segments, classId);
	t = clock() - t;
	if (DEBUG)
		printf("Tracking takes %d(ms)\n", t);

	int segmentIdxPrev = 0, segmentIdxNext = 1, targetIdxPrev = 0;
	Mat framePrev, frameNext;
	int compareHistMethods[4] = {CV_COMP_CORREL, CV_COMP_CHISQR, CV_COMP_INTERSECT, CV_COMP_BHATTACHARYYA };
	int compareHistMethod = compareHistMethods[0];
	double resizeRatio = 0.5;
	int marginTop = 200;
	namedWindow("prev");
	namedWindow("next");
	moveWindow("prev", 0, marginTop);
	moveWindow("next", cap.get(CV_CAP_PROP_FRAME_WIDTH) * resizeRatio, marginTop);

	bool switchM = true, switchS = true, switchF = false, switchB = false; // switch for similarity, forward, backward error
	while (true)
	{
		// set frame
		int prevFrameNum = frameStep * (LOW_LEVEL_TRACKLETS * segmentIdxPrev + (LOW_LEVEL_TRACKLETS / 2)) + startFrameNum;
		int nextFrameNum = frameStep * (LOW_LEVEL_TRACKLETS * segmentIdxNext + (LOW_LEVEL_TRACKLETS / 2)) + startFrameNum;
		cap.set(CV_CAP_PROP_POS_FRAMES, prevFrameNum);
		cap >> framePrev;
		cap.set(CV_CAP_PROP_POS_FRAMES, nextFrameNum);
		cap >> frameNext;

		// show tracklet
		if (segments.size() <= segmentIdxNext) break;
		Segment &segmentPrev = segments[segmentIdxPrev], &segmentNext = segments[segmentIdxNext];
		vector<tracklet> &trackletsPrev = segmentPrev.tracklets, &trackletsNext = segmentNext.tracklets;
		
		// check if targets exist
		if (trackletsPrev.size() == 0)
		{
			segmentIdxPrev++;
			continue;
		}
		if (trackletsNext.size() == 0)
		{
			segmentIdxNext++;
			continue;
		}

		// draw tracklets
		int objectId = 0;
		for (int i = 0; i < trackletsPrev.size(); i++)
		{
			tracklet &trackletPrev = trackletsPrev[i];
			for (int j = 0; j < trackletPrev.size(); j++) 
			{
				circle(framePrev, trackletPrev[j].getCenterPoint(), 2, WHITE, 2);
				circle(framePrev, trackletPrev[j].getCenterPoint(), 1, colors[objectId], 2);
			}
			objectId++;
		}
		for (int i = 0; i < trackletsNext.size(); i++) 
		{
			tracklet &trackletNext = trackletsNext[i];
			for (int j = 0; j < trackletNext.size(); j++) 
			{
				circle(frameNext, trackletNext[j].getCenterPoint(), 2, WHITE, 2);
				circle(frameNext, trackletNext[j].getCenterPoint(), 1, colors[objectId], 2);
			}
			objectId++;
		}
		tracklet& trackletPrev = trackletsPrev[targetIdxPrev];
		Target& targetPrev = trackletPrev[LOW_LEVEL_TRACKLETS / 2];
		rectangle(framePrev, targetPrev.getTargetArea(), colors[targetIdxPrev], 2);
		/*
		MatND histSumPrev = trackletsPrev[targetIdxPrev][0].hist;
		for (int i = 1; i < trackletsPrev[targetIdxPrev].size(); i++)
			histSumPrev += trackletsPrev[targetIdxPrev][i].hist;
		normalize(histSumPrev, histSumPrev, 0, 1, NORM_MINMAX, -1, Mat());
		*/
		// calculate similarities
		for (int i = 0; i < trackletsNext.size(); i++)
		{
			/*
			MatND histSumNext = trackletsNext[i][0].hist;;
			for (int j = 1; j < trackletsNext[i].size(); j++)
				histSumNext += trackletsNext[i][j].hist;
			normalize(histSumNext, histSumNext, 0, 1, NORM_MINMAX, -1, Mat());
			double similarityAppearance = compareHist(histSumPrev, histSumNext, compareHistMethod);			
			*/

			tracklet& trackletNext = trackletsNext[i];
			double similarityAppearance = calcSimilarityAppearance(trackletPrev, trackletNext, segmentIdxNext - segmentIdxPrev);
			Target & targetNext = trackletsNext[i][LOW_LEVEL_TRACKLETS / 2];
			if (switchS)
				putText(frameNext, to_string(similarityAppearance), targetNext.getCenterPoint(), CV_FONT_HERSHEY_SIMPLEX, 0.7, GREEN, 2);
			
			double similarityMotion = calcSimilarityMotion(trackletPrev, trackletNext, segmentIdxNext - segmentIdxPrev);
			if (switchM)
				putText(frameNext, to_string(similarityMotion), targetNext.getCenterPoint() + Point(0, 20), CV_FONT_HERSHEY_SIMPLEX, 0.7, RED, 2);
			
			double similarity = calcSimilarity(trackletPrev, trackletNext, segmentIdxNext - segmentIdxPrev);
			if (switchS && switchM)
				putText(frameNext, to_string(similarity), targetNext.getCenterPoint() + Point(0, 40), CV_FONT_HERSHEY_SIMPLEX, 0.7, BLUE, 2);
			
			double forwardDeviationError = calcForwardDeviationError(trackletPrev, trackletNext, segmentIdxNext - segmentIdxPrev);
			double backwardDeviationError = calcBackwardDeviationError(trackletPrev, trackletNext, segmentIdxNext - segmentIdxPrev);
			if (switchF)
				putText(frameNext, to_string(forwardDeviationError), targetNext.getCenterPoint() + Point(0, 60), CV_FONT_HERSHEY_SIMPLEX, 0.7, BLUE, 2);
			if (switchB)
				putText(frameNext, to_string(backwardDeviationError), targetNext.getCenterPoint() + Point(0, 80), CV_FONT_HERSHEY_SIMPLEX, 0.7, BLUE, 2);
		}

		// put frame number
		rectangle(framePrev, Rect(0, 0, 150, 30), WHITE, -1);
		rectangle(frameNext, Rect(0, 0, 150, 30), WHITE, -1);
		putText(framePrev, "Frame #" + to_string(prevFrameNum), Point(10, 20), CV_FONT_HERSHEY_SIMPLEX, 0.7, BLACK, 2);
		putText(frameNext, "Frame #" + to_string(nextFrameNum), Point(10, 20), CV_FONT_HERSHEY_SIMPLEX, 0.7, BLACK, 2);

		// show result
		resize(framePrev, framePrev, Size(framePrev.size().width * resizeRatio, framePrev.size().height * resizeRatio));
		resize(frameNext, frameNext, Size(frameNext.size().width * resizeRatio, frameNext.size().height * resizeRatio));
		imshow("prev", framePrev);
		imshow("next", frameNext);

		int key = waitKey(0);
		switch ((char)key) {
			case 'i': // prev Up
				if (segmentIdxPrev < segmentIdxNext)
					segmentIdxPrev++;
				while (segments[segmentIdxPrev].tracklets.size() == 0) { segmentIdxPrev++; }
				targetIdxPrev = 0;
				break;
			case 'o': // next Up
				if (segmentIdxNext + 1 < segments.size())
					segmentIdxNext++;
				while (segments[segmentIdxNext].tracklets.size() == 0) { segmentIdxNext++; }
				break;
			case 'k': // prev Down
				if (segmentIdxPrev > 0)
					segmentIdxPrev--;
				targetIdxPrev = 0;
				while (segments[segmentIdxPrev].tracklets.size() == 0) { segmentIdxPrev--; }
				break;
			case 'l': // next Down
				if (segmentIdxPrev < segmentIdxNext)
					segmentIdxNext--;
				while (segments[segmentIdxNext].tracklets.size() == 0) { segmentIdxNext--; }
				break;
			case 't': // move target
				targetIdxPrev = (targetIdxPrev + 1) % trackletsPrev.size();
				printf("targetIdx:%d\n", targetIdxPrev);
				break;
			case 'c':
				compareHistMethod = (compareHistMethod + 1) % 4;
				break;
			case 's':
				switchS = !switchS;
				break;
			case 'm':
				switchM = !switchM;
				break;
			case 'f':
				switchF = !switchF;
				break;
			case 'b':
				switchB = !switchB;
				break;
		}
	}
}