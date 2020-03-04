Lock-Free hash table implementation. CAS operations are used only to sync buckets, which allows threads working with different buckets to have less friction. Wait-Free count is also used, which allowed to abandon Interlocked.Add. All public methods are thread-safe. The enumerator does not lock the hash table while reading.
See for more information https://www.linkedin.com/pulse/lock-free-hash-table-maksim-burtsev/

![1](https://media-exp1.licdn.com/dms/image/C5612AQE5bSUX1VOctQ/article-inline_image-shrink_1000_1488/0?e=1588809600&v=beta&t=CdG1yFSUrRxMvddcczhsHGUkeCJzrArTT4JViFWvNik)
![2](https://media-exp1.licdn.com/dms/image/C5612AQG5iqeTwDripA/article-inline_image-shrink_1000_1488/0?e=1588809600&v=beta&t=RF4T49iEIu8CSQxnKn-EVVWuF1KOWapt7abJ_NwrRyc)
![3](https://media-exp1.licdn.com/dms/image/C5612AQEQJbOtSqhbHQ/article-inline_image-shrink_1500_2232/0?e=1588809600&v=beta&t=7FTeocICcaJ2DFSeC1Jct7KRbFwKeJhWtmQIdJGJ6Vk)
![4](https://media-exp1.licdn.com/dms/image/C5612AQGYJ9xFGkB0Nw/article-inline_image-shrink_1000_1488/0?e=1588809600&v=beta&t=DFQGtARBiTPGFUl4h-i1vqgVIeumE9swVFN4oTBlskU)
![5](https://media-exp1.licdn.com/dms/image/C5612AQFhyVRmr4Es8g/article-inline_image-shrink_1000_1488/0?e=1588809600&v=beta&t=dIg1KJLYMhCRn5S_USv3xomwr7VdTH6gm_yzNyTisnU)
![6](https://media-exp1.licdn.com/dms/image/C5612AQH7LCdXXYeNOA/article-inline_image-shrink_1500_2232/0?e=1588809600&v=beta&t=CNcoe0UfbA89WaZcOXid_rDSLDvfCrgFx1ndt_Qy1QM)