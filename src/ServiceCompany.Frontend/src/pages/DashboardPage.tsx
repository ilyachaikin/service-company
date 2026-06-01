import React, { useEffect, useState } from 'react';
import { Row, Col, Card, Statistic, List, Typography, Spin, Avatar } from 'antd';
import {
  FileTextOutlined,
  TeamOutlined,
  AlertOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined
} from '@ant-design/icons';
import { Pie, Column } from '@ant-design/plots';
import api from '../api/client';
import dayjs from 'dayjs';
import { useTheme } from '../store/ThemeContext';

const { Title, Text } = Typography;

const DashboardPage: React.FC = () => {
  const { isDark } = useTheme();
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/Dashboard/stats')
      .then(res => setData(res.data))
      .catch(err => console.error(err))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div style={{ textAlign: 'center', marginTop: 100 }}><Spin size="large" tip="Загрузка статистики..." /></div>;
  if (!data) return <div>Не удалось загрузить статистику</div>;

  const statusLabels: Record<string, string> = {
    New:              'Новая',
    Assigned:         'Назначена',
    InProgress:       'В работе',
    WaitingForParts:  'Ожидание запчастей',
    Done:             'Выполнена',
    Closed:           'Закрыта',
    Cancelled:        'Отменена',
    Archived:         'Архив',
  };

  const textColor = isDark ? '#e0e0e0' : '#333333';
  const gridColor = isDark ? 'rgba(255,255,255,0.15)' : 'rgba(0,0,0,0.1)';

  const pieData = (data.statusDistribution as { label: string; value: number }[])
    .filter(d => d.value > 0)
    .map(d => ({ label: statusLabels[d.label] ?? d.label, value: d.value }));

  const pieConfig = {
    data: pieData,
    angleField: 'value',
    colorField: 'label',
    theme: isDark ? 'dark' : 'light',
    label: {
      text: (d: any) => `${d.label}\n${d.value}`,
      position: 'outside',
      style: { fill: textColor, fontSize: 12 },
    },
    legend: {
      color: {
        title: false,
        position: 'bottom',
        itemLabelFill: textColor,
      },
    },
    tooltip: {
      items: [{ field: 'value', name: 'Заявок' }],
    },
  };

  const fmtTenge = (v: number) => {
    if (v >= 1_000_000) return `${(v / 1_000_000).toFixed(1)}М ₸`;
    if (v >= 1_000)     return `${(v / 1_000).toFixed(0)}К ₸`;
    return `${v} ₸`;
  };

  const columnConfig = {
    data: data.revenueStats,
    xField: 'date',
    yField: 'amount',
    theme: isDark ? 'dark' : 'light',
    label: {
      text: (d: any) => fmtTenge(d.amount),
      style: { fill: '#ffffff', opacity: 0.9 },
      position: 'inside',
    },
    axis: {
      y: {
        labelFormatter: (v: number) => fmtTenge(v),
        title: 'Выручка, ₸',
        style: {
          titleFill: textColor,
          labelFill: textColor,
          gridStroke: gridColor,
        },
      },
      x: {
        title: 'Месяц',
        style: {
          titleFill: textColor,
          labelFill: textColor,
        },
      },
    },
    tooltip: {
      items: [
        {
          field: 'amount',
          name: 'Выручка',
          valueFormatter: (v: number) => `${v.toLocaleString('ru-KZ')} ₸`,
        },
      ],
    },
  };

  return (
    <div>
      <Title level={2}>Главная</Title>

      {}
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} className="stat-card">
            <Statistic title="Всего заявок" value={data.summary.totalTickets} prefix={<FileTextOutlined />} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} className="stat-card">
            <Statistic title="Активные заявки" value={data.summary.activeTickets} valueStyle={{ color: '#3f8600' }} prefix={<ClockCircleOutlined />} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} className="stat-card">
            <Statistic title="Аварийные" value={data.summary.emergencyTickets} valueStyle={{ color: '#cf1322' }} prefix={<AlertOutlined />} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={4}>
          <Card bordered={false} className="stat-card">
            <Statistic title="Клиентов" value={data.summary.totalClients} prefix={<TeamOutlined />} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} className="stat-card">
            <Statistic title="Нарушения SLA" value={data.summary.slaBreaches} valueStyle={{ color: '#cf1322' }} prefix={<AlertOutlined />} />
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
        {}
        <Col xs={24} lg={12}>
          <Card title="Распределение заявок по статусам" bordered={false}>
            <div className="chart-container" style={{ height: 300 }}>
               <Pie {...pieConfig} />
            </div>
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card title="Динамика выручки" bordered={false}>
            <div className="chart-container" style={{ height: 300 }}>
                <Column {...columnConfig} />
            </div>
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
        {}
        <Col span={24}>
          <Card title="Последние события" bordered={false}>
            <List
              dataSource={data.recentActivity}
              renderItem={(item: any) => (
                <List.Item>
                  <List.Item.Meta
                    avatar={<Avatar icon={<CheckCircleOutlined />} style={{ backgroundColor: '#1890ff' }} />}
                    title={<Text strong>{item.action}</Text>}
                    description={`${item.description} — ${item.user}`}
                  />
                  <Text type="secondary">{dayjs(item.createdAt).fromNow()}</Text>
                </List.Item>
              )}
            />
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default DashboardPage;
