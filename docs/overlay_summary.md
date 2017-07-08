# Overlay Summary

RapidCheck tech documents about Detection Engine powered by Junsu Lee (ljs93kr@gmail.com)

#### 개요

  RapidCheck 의 병렬화 알고리즘은 영상의 객체정보가 분석이 완료된다면 기존의 검색방법처럼 "사람이 하나하나 순차적으로 확인할 필요가 없다" 는 아이디어에서 착안하다.

  기존의 기술로는, 원하는 객체 출연 시점이 언제인지를 모르기 때문에 긴 영상을 처음부터 확인해야 했고, 모든 순간 을 집중적으로 확인해야할 필요가 있지 않기 때문에 가장 원초적인 배속검색을 수행하고 있었다. 하지만 순차배속검 색은 다음과 같은 한계를 가지고 있다.

```
- 영상 플레이 속도를 높히기 때문에, 장시간 확인하고 있을 시 집중도가 급격하게 떨어지게되며, 이는 원하는 객체를 놓치고도 놓친지 알 수 없음
- 실제 10분 이상 배속 검색을 수행하는 사람의 오탐율이 급격히 증가하는 연구 자료가 있음
- 한 사람 당 하나의 영상만 확인할 수 있음
```

  현장에서는 이러한 문제점을 모르는 바가 아니나, 마땅한 기술적 지원이 없어서 현재 이 불필요한 에너지소모를 지속 하고 있는 실정이다.



#### 객체 출연 시점의 병렬화 알고리즘

  RapidCheck 디스플레이 엔진은 위와 같은 문제를 해결하고자 "객체 출연 시점의 병렬화 알고리즘" 을 구현하다. 이는 서로 다른 시점에 출연한 객체일지라도 보여줄때는 한번에 합쳐서 동시에 확인이 가능하도록 만드는 기술이다.

[![스크린샷 2017-07-02 오후 12.48.15.png](https://s19.postimg.org/rn8eg2oyr/2017-07-02_12.48.15.png)](https://postimg.org/image/ogduwg4in/)

*RapidCheck 병렬화 디스플레이 개념*

 위 사진은 실제 RapidCheck 디스플레이 엔진의 결과물이다. 원본 영상은 60분에 달하는 긴 영상이지만, RapidCheck에서는 단 2~3분 안에 등장하는 객체들의 모습을 한눈에 확인이 가능하다. 배속 효과 없이도 배속 이상의 시간 단축 효과를 만들어낼 수 있는데, 실제 플레이 속도를 높히는 방법이 아니기 때문에, 사용자의 오탐율을 크 게 줄일 수 있다. 추가로 합성한 영상에 대해서도 플레이 속도를 사용자 입맛에 맞게 조절할 수 있으므로, 15배~30배 이상의 시간 단축 효과를 기대할 수 있다.

```
- 정상 플레이 속도로도 시간 단축의 효과
- 방향, 색상, 종류 등의 필터들로 인해서 인접한 객체만 확인할 수 있기 때문에, 사용자의 피로감을 줄이 고, 오탐율을 극소화
- 합성 영상 플레이의 속도 역시 조절이 가능 
```

#### 출연 시점 바로가기

  RapidCheck 의 비디오 플레이어는 사용자의 입력(User Interface)을 받을 수 있다. 영상이 플레이될 때 객체를 합성하는 과정은 실시간으로 진행이 된다. 따라서 사용자는 원하는 객체를 합성영상에서 발견했을 시, 그 객체를 클릭함으로써 해당 객체가 출연하는 시점으로 영상을 바로 이동 및 확인할 수 있다. 

[![스크린샷 2017-07-02 오후 1.18.00.png](https://s19.postimg.org/l9od9xqeb/2017-07-02_1.18.00.png)](https://postimg.org/image/7srer2g2n/)

*원본 즉시 확인 가능*

  사용자는 한번의 클릭으로 영상속에서 해당위치를 바로 찾아갈 수 있기 때문에 한방검색(One-Shot Searching) 이 라고 할 수 있다. 이로써 필요한 화면을 정확하게 찾아주어 피로감을 줄이고, 빠르게 확인할 수 있어 시간 단축에 효과적이다.

#### K-Means Multi Queue Algorithm

  객체들을 병렬적으로 겹쳐서 보여주는 RapidCheck 디스플레이 아이디어를 실현하였을때, 한가지 이슈가 발생했 다. 객체 출연 시점을 단순비교하여 화면에 겹쳐서 보여주게 되면, 완전히 동일한 위치에 출연하는 객체들이 매우 겹 치면서 오히려 판독하기가 힘들어진 것이다. 타 영상 분석 솔루션이 디스플레이 방법을 정할 때 다음과 같은 이슈로 인해서 본 아이디어를 채택하지 않았다.

```
- 객체가 겹쳐질 때, 배경과 객체간의 부자연스러운 모습이 연출됨
- 한번에 너무 많은 객체가 그려질 때, 오히려 판독하기 힘들어짐
– 다양한 환경에서 객체 등장 위치를 일일이 사람이 정해주기 힘듦
```

  RapidCheck 디스플레이 엔진은 겹쳐질 때 배경과의 부자연스러운 모습을 "잔상 제거" 기술을 개발하여 해결하고, 한번에 많은 객체가 그려질 수 있는 상황과, 객체 등장 위치를 비지도학습(Unsupervised Learning) 방법 중 K-군집화(K-Means Clustering)를 응용하여 해결하다. 객체 등장 위치를 기반으로 K-군집화 기능을 통과하게 되면, 사람이 등장 시점이나 위치를 지정해주지 않아도, 어떤 영상이든 객체가 알아서 적절한 위치로 군집화를 이루게 된다. 따라서 사용자는 특별한 작업없이도 서로 다른 등장 시점의 객체들을 한번에 확인이 가능하다. 이것이 K-군집화를 기반으로하여 병렬화 알고리즘의 문제를 해결한 RapidCheck 디스플레이 엔진의 독창적 가치이다.

[![pic.jpg](https://s19.postimg.org/r27d6yn6r/pic.jpg)](https://postimg.org/image/6utxenppb/)

*좌측부터 밀도값 3,6,9 설정 결과 화면*

RapidCheck 디스플레이 엔진의 우수성을 정리하면 다음과 같다.

```
- 타 객체와 배경과의 부자연스러운 모습은 RapidCheck 의 Alpha Blending 과 잔상제거 기술로 가시효과를 높힘
- 비지도학습 방법인 K-군집화 알고리즘과 우선순위 정렬 기법을 결합하여, 여러 객체가 겹치는 상황에서도 사용자들이 판독하기에 불편함을 최소화함
- 영상 종류에 관계없이 "알아서" 객체 등장 위치가 고르게 퍼지는 효과
```
