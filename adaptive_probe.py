import cv2
import numpy as np
from pathlib import Path

OK = Path(r"E:\source\date\class\OK")
BAD = Path(r"E:\source\date\class\NG\验证图片\待复检")

def read(path):
    return cv2.imdecode(np.fromfile(str(path), dtype=np.uint8), cv2.IMREAD_GRAYSCALE)

def smooth(v, radius):
    if radius <= 0:
        return v.astype(np.int32)
    return np.convolve(v, np.ones(radius * 2 + 1) / (radius * 2 + 1), mode="same").astype(np.int32)

def span(v, threshold, radius, support):
    active = v >= threshold
    supported = np.convolve(active.astype(np.int32), np.ones(radius * 2 + 1, np.int32), mode="same") >= support
    ids = np.flatnonzero(supported)
    if ids.size:
        return int(ids[0]), int(ids[-1] + 1)
    ids = np.flatnonzero(active)
    return (int(ids[0]), int(ids[-1] + 1)) if ids.size else (0, len(v))

def regions(gray):
    h, w = gray.shape
    dark = gray < 155
    xs = span(smooth(dark.sum(0), 8), max(8, int(h * .025)), 8, 5)
    ys = span(smooth(dark.sum(1), 5), max(20, int(w * .04)), 6, 4)
    x1, x2 = xs; y1, y2 = ys
    mw, mh = x2-x1, y2-y1
    outer = round(mw*.045); inner = round(mw*.335)
    top = round(mh*.07); bottom = round(mh*.06)
    return [(x1+outer, y1+top, inner-outer, mh-top-bottom),
            (x2-inner, y1+top, inner-outer, mh-top-bottom)]

def odd(v, limit):
    v = max(3, min(int(v), int(limit)))
    if v % 2 == 0: v -= 1
    return max(3, v)

def extract(gray):
    sides=[]
    for x,y,w,h in regions(gray):
        roi=gray[y:y+h,x:x+w]
        blur=cv2.GaussianBlur(roi,(3,3),0)
        otsu,_=cv2.threshold(blur,0,255,cv2.THRESH_BINARY+cv2.THRESH_OTSU)
        threshold=max(115,min(190,otsu))
        mask=(blur>threshold).astype(np.uint8)*255
        close=odd(min(w,h)*.035,min(w,h)); op=odd(min(w,h)*.012,min(w,h))
        mask=cv2.morphologyEx(mask,cv2.MORPH_CLOSE,cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(close,close)))
        mask=cv2.morphologyEx(mask,cv2.MORPH_OPEN,cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(op,op)))
        norm_mask=cv2.resize(mask,(64,96),interpolation=cv2.INTER_NEAREST)>0
        bgk=odd(min(w,h)*.13,min(w,h))
        bh=cv2.morphologyEx(blur,cv2.MORPH_BLACKHAT,cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(bgk,bgk)))
        vals=blur[mask>0]
        median=float(np.median(vals)) if vals.size else float(np.median(blur))
        scale=max(20.0,float(np.percentile(vals,75)-np.percentile(vals,25))) if vals.size else 20.0
        nbh=bh.astype(np.float32)/scale
        norm_bh=cv2.resize(nbh,(64,96),interpolation=cv2.INTER_AREA)
        core=cv2.erode(mask,cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(odd(min(w,h)*.08,min(w,h)),)*2))>0
        bvals=nbh[core]
        dark_ratio=float(np.mean(blur[core] < median-55)) if np.any(core) else 1.0
        normalized=cv2.resize(blur,(64,96),interpolation=cv2.INTER_AREA)
        normalized=cv2.createCLAHE(2.0,(8,8)).apply(normalized)
        sides.append(dict(mask=norm_mask,bh=norm_bh,image=normalized,coverage=float(mask.mean()/255),
                          p90=float(np.percentile(bvals,90)),p95=float(np.percentile(bvals,95)),
                          p99=float(np.percentile(bvals,99)),dark=dark_ratio,otsu=float(otsu)))
    return sides

def files(path, bmp_only=False):
    ext={'.bmp'} if bmp_only else {'.bmp','.png','.jpg','.jpeg'}
    return sorted([p for p in path.iterdir() if p.suffix.lower() in ext and '_改' not in p.name])

ok_files=files(OK)
bad_files=files(BAD,True)
ok_samples=[extract(read(p)) for p in ok_files]
bad_samples=[extract(read(p)) for p in bad_files]

prob=[]
for side in range(2):
    prob.append(np.mean([s[side]['mask'] for s in ok_samples],axis=0))

def vector(sample):
    out=[]
    for side in range(2):
        s=sample[side]; p=prob[side]
        missing=float(np.mean((p>.80)&(~s['mask'])))
        diff=float(np.mean(np.abs(s['mask'].astype(float)-p)))
        out.extend([s['coverage'],missing,diff,s['p90'],s['p95'],s['p99'],s['dark'],s['otsu']])
    return np.array(out)

X=np.array([vector(s) for s in ok_samples]); Y=np.array([vector(s) for s in bad_samples])
names=['cov','missing','diff','p90','p95','p99','dark','otsu']*2
lo=np.percentile(X,1,axis=0); q50=np.percentile(X,50,axis=0); hi=np.percentile(X,99,axis=0)
scale=np.maximum((hi-lo)/2,1e-5)
def score(z):
    low=np.maximum(0,(lo-z)/scale); high=np.maximum(0,(z-hi)/scale)
    # low coverage is anomalous; all other features may be anomalous on either tail except otsu.
    parts=np.maximum(low,high)
    parts[[7,15]]=0
    return float(np.max(parts)), int(np.argmax(parts)), parts

oks=np.array([score(z)[0] for z in X]); bads=np.array([score(z)[0] for z in Y])
print('COUNTS',len(ok_files),len(bad_files))
print('OK score percentiles',np.percentile(oks,[50,80,90,95,99,100]))
print('BAD score percentiles',np.percentile(bads,[0,10,25,50,75,90,100]))
for p,z in zip(bad_files,Y):
    s,i,_=score(z); print('BAD',p.name,round(s,3),names[i],round(z[i],3),'normal',round(lo[i],3),round(hi[i],3))
for idx in np.argsort(oks)[-15:]:
    s,i,_=score(X[idx]); print('OKHIGH',ok_files[idx].name,round(s,3),names[i])

hog=cv2.HOGDescriptor((64,96),(16,16),(8,8),(8,8),9)
def hfeature(sample):
    z=np.concatenate([hog.compute(s['image']).ravel() for s in sample]).astype(np.float32)
    z/=max(float(np.linalg.norm(z)),1e-8)
    return z
HX=np.array([hfeature(s) for s in ok_samples]); HY=np.array([hfeature(s) for s in bad_samples])
similar=HX@HX.T; np.fill_diagonal(similar,-1)
okdist=1-similar.max(1)
baddist=1-(HY@HX.T).max(1)
print('HOG OK percentiles',np.percentile(okdist,[0,50,80,90,95,99,100]))
print('HOG BAD percentiles',np.percentile(baddist,[0,10,25,50,75,90,100]))
for p,d in zip(bad_files,baddist): print('HOG_BAD',p.name,round(float(d),5))

def largest_ratio(binary):
    n,lab,stats,_=cv2.connectedComponentsWithStats(binary.astype(np.uint8),8)
    if n<=1: return 0.0
    return float(stats[1:,cv2.CC_STAT_AREA].max()/binary.size)

def location_scores(sample):
    sm=[]; tm=[]
    for side,s in enumerate(sample):
        expected=prob[side]>.80
        missing=expected & (~s['mask'])
        missing=cv2.morphologyEx(missing.astype(np.uint8),cv2.MORPH_OPEN,np.ones((3,3),np.uint8))>0
        core=cv2.erode(s['mask'].astype(np.uint8),np.ones((5,5),np.uint8))>0
        threshold=max(1.8,float(np.percentile(s['bh'][core],99.0))) if np.any(core) else 99
        texture=(s['bh']>threshold)&core
        texture=cv2.morphologyEx(texture.astype(np.uint8),cv2.MORPH_CLOSE,np.ones((3,3),np.uint8))>0
        sm.append(largest_ratio(missing)); tm.append(largest_ratio(texture))
    return max(sm),max(tm)
LO=np.array([location_scores(s) for s in ok_samples]); LB=np.array([location_scores(s) for s in bad_samples])
print('LOC OK shape',np.percentile(LO[:,0],[50,90,95,99,100]),'texture',np.percentile(LO[:,1],[50,90,95,99,100]))
print('LOC BAD shape',np.percentile(LB[:,0],[0,25,50,75,100]),'texture',np.percentile(LB[:,1],[0,25,50,75,100]))
for p,(a,b) in zip(bad_files,LB): print('LOC_BAD',p.name,round(a,5),round(b,5))
