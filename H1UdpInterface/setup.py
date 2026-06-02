from setuptools import setup, find_packages

setup(
    name='h1_udp',
    version='0.1.0',
    description='MuJoCo兼容风格的Unity Humanoid/多关节UDP控制仿真接口',
    author='你的名字',
    author_email='your@email.com',
    packages=find_packages(),
    python_requires='>=3.6',
    install_requires=[],  # numpy将被示例用到，仅在demo中soft依赖
    url='',
    long_description=open('README.md', encoding='utf-8').read(),
    long_description_content_type='text/markdown',
    classifiers=[
        'Programming Language :: Python :: 3',
        'Intended Audience :: Developers',
        'Operating System :: OS Independent',
    ],
)
